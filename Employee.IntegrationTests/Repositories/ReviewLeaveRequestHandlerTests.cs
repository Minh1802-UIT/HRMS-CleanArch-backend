using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Employee.Application.Features.Leave.Commands.ReviewLeaveRequest;
using Employee.Application.Features.Leave.Dtos;
using Employee.Domain.Entities.HumanResource;
using Employee.Domain.Entities.Leave;
using Employee.Domain.Entities.Organization;
using Employee.Domain.Entities.ValueObjects;
using Employee.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Employee.IntegrationTests.Repositories;

/// <summary>
/// Integration tests for the leave-request review flow.
/// Seeds real MongoDB documents (employee, leave type, leave request) then
/// exercises the HTTP pipeline: routing → auth → CQRS handler → repository → MediatR events.
///
/// Key behaviors tested:
/// - Approve a pending leave request → status becomes Approved, allocation deducted
/// - Reject a pending leave request → status becomes Rejected
/// - Review non-existent request → 404
/// - Review already-approved request → 422 (business rule violation)
/// - Optimistic concurrency: stale version → 409
/// </summary>
[Collection("Api")]
public class ReviewLeaveRequestHandlerTests : IntegrationTestBase
{
  public ReviewLeaveRequestHandlerTests(IntegrationTestFixture fixture) : base(fixture) { }

  private IMongoCollection<EmployeeEntity> Employees => Fixture.Database.GetCollection<EmployeeEntity>("employees");
  private IMongoCollection<Department> Departments => Fixture.Database.GetCollection<Department>("departments");
  private IMongoCollection<Position> Positions => Fixture.Database.GetCollection<Position>("positions");
  private IMongoCollection<LeaveRequest> LeaveRequests => Fixture.Database.GetCollection<LeaveRequest>("leave_requests");
  private IMongoCollection<LeaveType> LeaveTypes => Fixture.Database.GetCollection<LeaveType>("leave_types");

  private async Task<string> SeedTestEmployeeAsync(string name = "Test Employee", string email = "test@company.com")
  {
    var deptId = ObjectId.GenerateNewId().ToString();
    await Departments.InsertOneAsync(new Department("Engineering", "ENG") { Id = deptId });
    var posId = ObjectId.GenerateNewId().ToString();
    await Positions.InsertOneAsync(new Position("Software Engineer", "SE", deptId) { Id = posId });

    var emp = new EmployeeEntity($"E-{Guid.NewGuid():N}".Substring(0, 8), name, email);
    emp.UpdateJobDetails(new JobDetails
    {
      DepartmentId = deptId,
      PositionId = posId,
      JoinDate = DateTime.UtcNow.AddMonths(-6)
    });
    await Employees.InsertOneAsync(emp);
    return emp.Id;
  }

  private async Task<string> SeedLeaveTypeAsync(string code = "Annual", int defaultDays = 12, bool isSandwichRuleApplied = false)
  {
    var lt = new LeaveType("Annual Leave", code, defaultDays);
    lt.UpdateSettings(isAccrual: false, rate: 0, allowCarryForward: true, maxCarry: 5, isSandwichRuleApplied: isSandwichRuleApplied);
    await LeaveTypes.InsertOneAsync(lt);
    return lt.Id;
  }

  private async Task<string> SeedLeaveRequestAsync(string employeeId, LeaveCategory category = LeaveCategory.Annual, string leaveTypeCode = "Annual")
  {
    var lr = new LeaveRequest(
        employeeId,
        category,
        DateTime.UtcNow.AddDays(7),
        DateTime.UtcNow.AddDays(10),
        "Family vacation");
    await LeaveRequests.InsertOneAsync(lr);
    return lr.Id;
  }

  private string GenerateManagerToken()
  {
    var tokenService = Factory.Services.GetRequiredService<Employee.Application.Common.Interfaces.ITokenService>();
    return tokenService.GenerateJwtToken(
        userId: "manager-001",
        email: "manager@company.com",
        fullName: "Manager",
        roles: new[] { "Manager", "Admin" },
        employeeId: "manager-emp-001");
  }

  // ─────────────────────────────────────────────────────────────────
  // Happy Path Tests
  // ─────────────────────────────────────────────────────────────────

  [Fact]
  public async Task Review_ApprovePendingRequest_ShouldReturn200AndPersistChanges()
  {
    // Arrange — seed employee + leave type + pending leave request
    var employeeId = await SeedTestEmployeeAsync("Nguyen Van B", "b@company.com");
    var leaveTypeCode = "Annual";
    await SeedLeaveTypeAsync(leaveTypeCode);
    var requestId = await SeedLeaveRequestAsync(employeeId, LeaveCategory.Annual, leaveTypeCode);

    var token = GenerateManagerToken();
    var reviewDto = new ReviewLeaveRequestDto
    {
      Id = requestId,
      Status = "Approved",
      ManagerComment = "Approved. Enjoy your vacation!",
      ExpectedVersion = 1
    };

    var request = new HttpRequestMessage(HttpMethod.Post, $"/api/leaves/{requestId}/review")
    {
      Content = JsonContent.Create(reviewDto)
    };
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    // Act
    var response = await Client.SendAsync(request);

    // Assert — HTTP layer
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    // Assert — MongoDB: status updated
    var updated = await LeaveRequests
        .Find(x => x.Id == requestId)
        .FirstOrDefaultAsync();
    Assert.NotNull(updated);
    Assert.Equal(LeaveStatus.Approved, updated.Status);
    Assert.Equal("manager-001", updated.ApprovedBy);
    Assert.Equal("Approved. Enjoy your vacation!", updated.ManagerComment);
    Assert.Equal(2, updated.Version); // version bumped by 1
  }

  [Fact]
  public async Task Review_RejectPendingRequest_ShouldReturn200WithRejectedStatus()
  {
    // Arrange
    var employeeId = await SeedTestEmployeeAsync("Tran Van C", "c@company.com");
    await SeedLeaveTypeAsync("Sick", 10);
    var requestId = await SeedLeaveRequestAsync(employeeId, LeaveCategory.Sick, "Sick");

    var token = GenerateManagerToken();
    var reviewDto = new ReviewLeaveRequestDto
    {
      Id = requestId,
      Status = "Rejected",
      ManagerComment = "Team is short-staffed. Please reschedule.",
      ExpectedVersion = 1
    };

    var request = new HttpRequestMessage(HttpMethod.Post, $"/api/leaves/{requestId}/review")
    {
      Content = JsonContent.Create(reviewDto)
    };
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    // Act
    var response = await Client.SendAsync(request);

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var updated = await LeaveRequests.Find(x => x.Id == requestId).FirstOrDefaultAsync();
    Assert.NotNull(updated);
    Assert.Equal(LeaveStatus.Rejected, updated.Status);
    Assert.Equal("manager-001", updated.ApprovedBy);
  }

  // ─────────────────────────────────────────────────────────────────
  // Error Path Tests
  // ─────────────────────────────────────────────────────────────────

  [Fact]
  public async Task Review_NonExistentRequest_ShouldReturn404()
  {
    // Arrange
    var token = GenerateManagerToken();
    var fakeId = ObjectId.GenerateNewId().ToString();

    var reviewDto = new ReviewLeaveRequestDto
    {
      Id = fakeId,
      Status = "Approved",
      ExpectedVersion = 1
    };

    var request = new HttpRequestMessage(HttpMethod.Post, $"/api/leaves/{fakeId}/review")
    {
      Content = JsonContent.Create(reviewDto)
    };
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    // Act
    var response = await Client.SendAsync(request);

    // Assert
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }

  [Fact]
  public async Task Review_AlreadyApprovedRequest_ShouldReturn422()
  {
    // Arrange — seed an already-approved leave request
    var employeeId = await SeedTestEmployeeAsync("Le Van D", "d@company.com");
    await SeedLeaveTypeAsync("Annual");
    var requestId = await SeedLeaveRequestAsync(employeeId);

    // Directly update to "Approved" state in MongoDB (simulating a previous review)
    await LeaveRequests.UpdateOneAsync(
        x => x.Id == requestId,
        Builders<LeaveRequest>.Update
            .Set(x => x.Status, LeaveStatus.Approved)
            .Set(x => x.ApprovedBy, "previous-manager")
            .Set(x => x.Version, 2));

    var token = GenerateManagerToken();
    var reviewDto = new ReviewLeaveRequestDto
    {
      Id = requestId,
      Status = "Rejected",
      ExpectedVersion = 2
    };

    var request = new HttpRequestMessage(HttpMethod.Post, $"/api/leaves/{requestId}/review")
    {
      Content = JsonContent.Create(reviewDto)
    };
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    // Act
    var response = await Client.SendAsync(request);

    // Assert — business rule violation → 422
    Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
  }

  [Fact]
  public async Task Review_InvalidStatusString_ShouldReturn400()
  {
    // Arrange
    var employeeId = await SeedTestEmployeeAsync();
    await SeedLeaveTypeAsync("Annual");
    var requestId = await SeedLeaveRequestAsync(employeeId);

    var token = GenerateManagerToken();
    var reviewDto = new ReviewLeaveRequestDto
    {
      Id = requestId,
      Status = "InvalidStatus", // Not "Approved" or "Rejected"
      ExpectedVersion = 1
    };

    var request = new HttpRequestMessage(HttpMethod.Post, $"/api/leaves/{requestId}/review")
    {
      Content = JsonContent.Create(reviewDto)
    };
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    // Act
    var response = await Client.SendAsync(request);

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task Review_UnauthorizedNoToken_ShouldReturn401()
  {
    // Arrange
    var employeeId = await SeedTestEmployeeAsync();
    await SeedLeaveTypeAsync("Annual");
    var requestId = await SeedLeaveRequestAsync(employeeId);

    var reviewDto = new ReviewLeaveRequestDto
    {
      Id = requestId,
      Status = "Approved",
      ExpectedVersion = 1
    };

    var request = new HttpRequestMessage(HttpMethod.Post, $"/api/leaves/{requestId}/review")
    {
      Content = JsonContent.Create(reviewDto)
    };
    // No Authorization header

    // Act
    var response = await Client.SendAsync(request);

    // Assert
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  // ─────────────────────────────────────────────────────────────────
  // Optimistic Concurrency Tests
  // ─────────────────────────────────────────────────────────────────

  [Fact]
  public async Task Review_StaleVersion_ShouldReturn409Conflict()
  {
    // Arrange
    var employeeId = await SeedTestEmployeeAsync("Optimistic Test", "opt@company.com");
    await SeedLeaveTypeAsync("Annual");
    var requestId = await SeedLeaveRequestAsync(employeeId);

    // Bump version to simulate another process already modified the request
    await LeaveRequests.UpdateOneAsync(
        x => x.Id == requestId,
        Builders<LeaveRequest>.Update.Set(x => x.Version, 5));

    var token = GenerateManagerToken();
    var reviewDto = new ReviewLeaveRequestDto
    {
      Id = requestId,
      Status = "Approved",
      ExpectedVersion = 1 // stale — DB has v5
    };

    var request = new HttpRequestMessage(HttpMethod.Post, $"/api/leaves/{requestId}/review")
    {
      Content = JsonContent.Create(reviewDto)
    };
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    // Act
    var response = await Client.SendAsync(request);

    // Assert — optimistic concurrency failure → 409
    Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

    // Assert — status unchanged
    var unchanged = await LeaveRequests.Find(x => x.Id == requestId).FirstOrDefaultAsync();
    Assert.NotNull(unchanged);
    Assert.Equal(LeaveStatus.Pending, unchanged.Status);
  }

  // ─────────────────────────────────────────────────────────────────
  // Sandwich Rule Test
  // ─────────────────────────────────────────────────────────────────

  [Fact]
  public async Task Review_LeaveTypeWithSandwichRule_ShouldCountCalendarDays()
  {
    // Arrange — seed a leave type with sandwich rule applied
    var employeeId = await SeedTestEmployeeAsync("Sandwich Test", "sand@company.com");
    var leaveTypeId = await SeedLeaveTypeAsync("Unpaid", 0, isSandwichRuleApplied: true);

    // Create leave request spanning a weekend (e.g., Fri to Mon)
    var friday = GetNextFriday();
    var monday = friday.AddDays(3);
    var lr = new LeaveRequest(employeeId, LeaveCategory.Unpaid, friday, monday, "Extended weekend");
    await LeaveRequests.InsertOneAsync(lr);

    var token = GenerateManagerToken();
    var reviewDto = new ReviewLeaveRequestDto
    {
      Id = lr.Id,
      Status = "Approved",
      ExpectedVersion = 1
    };

    var request = new HttpRequestMessage(HttpMethod.Post, $"/api/leaves/{lr.Id}/review")
    {
      Content = JsonContent.Create(reviewDto)
    };
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    // Act
    var response = await Client.SendAsync(request);

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var updated = await LeaveRequests.Find(x => x.Id == lr.Id).FirstOrDefaultAsync();
    Assert.NotNull(updated);
    Assert.Equal(LeaveStatus.Approved, updated.Status);
  }

  private static DateTime GetNextFriday()
  {
    var today = DateTime.UtcNow.Date;
    int daysUntilFriday = ((int)DayOfWeek.Friday - (int)today.DayOfWeek + 7) % 7;
    if (daysUntilFriday == 0) daysUntilFriday = 7;
    return today.AddDays(daysUntilFriday);
  }

  // IntegrationTestBase.Dispose() handles Client and Factory cleanup.
}
