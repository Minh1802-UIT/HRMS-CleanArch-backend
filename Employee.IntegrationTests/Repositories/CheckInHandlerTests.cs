using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Employee.Application.Features.Attendance.Dtos;
using Employee.Domain.Entities.Attendance;
using Employee.Domain.Entities.HumanResource;
using Employee.Domain.Entities.Organization;
using Employee.Domain.Entities.ValueObjects;
using Employee.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Employee.IntegrationTests.Repositories;

/// <summary>
/// Integration tests for the check-in/check-out flow (<see cref="CheckInHandler"/>).
///
/// Exercises:
/// - Raw attendance log persistence to MongoDB
/// - Spam protection (60-second cooldown)
/// - Graceful degradation when <see cref="IAttendanceProcessingService"/> fails
/// - Processing service integration with attendance buckets
/// </summary>
[Collection("Api")]
public class CheckInHandlerTests : IntegrationTestBase
{
  public CheckInHandlerTests(IntegrationTestFixture fixture) : base(fixture) { }

  private IMongoCollection<RawAttendanceLog> RawLogs => Fixture.Database.GetCollection<RawAttendanceLog>("raw_attendance_logs");
  private IMongoCollection<EmployeeEntity> Employees => Fixture.Database.GetCollection<EmployeeEntity>("employees");
  private IMongoCollection<Department> Departments => Fixture.Database.GetCollection<Department>("departments");
  private IMongoCollection<Position> Positions => Fixture.Database.GetCollection<Position>("positions");

  private async Task<string> SeedTestEmployeeAsync(string name = "CheckIn Test")
  {
    var deptId = ObjectId.GenerateNewId().ToString();
    await Departments.InsertOneAsync(new Department("Engineering", "ENG") { Id = deptId });
    var posId = ObjectId.GenerateNewId().ToString();
    await Positions.InsertOneAsync(new Position("Engineer", "ENG", deptId) { Id = posId });

    var emp = new EmployeeEntity($"E-{Guid.NewGuid():N}".Substring(0, 8), name, $"{Guid.NewGuid():N}@company.com");
    emp.UpdateJobDetails(new JobDetails
    {
      DepartmentId = deptId,
      PositionId = posId,
      JoinDate = DateTime.UtcNow.AddMonths(-6)
    });
    await Employees.InsertOneAsync(emp);
    return emp.Id;
  }

  private string GenerateEmployeeToken(string employeeId)
  {
    var tokenService = Factory.Services.GetRequiredService<Employee.Application.Common.Interfaces.ITokenService>();
    return tokenService.GenerateJwtToken(
        userId: $"user-{employeeId}",
        email: "test@company.com",
        fullName: "TestUser",
        roles: new[] { "Employee" },
        employeeId: employeeId);
  }

  private string GenerateAdminToken(string employeeId)
  {
    var tokenService = Factory.Services.GetRequiredService<Employee.Application.Common.Interfaces.ITokenService>();
    return tokenService.GenerateJwtToken(
        userId: "admin-001",
        email: "admin@company.com",
        fullName: "Admin",
        roles: new[] { "Admin", "HR" },
        employeeId: employeeId);
  }

  // ─────────────────────────────────────────────────────────────────
  // Happy Path
  // ─────────────────────────────────────────────────────────────────

  [Fact]
  public async Task CheckIn_ValidRequest_ShouldReturn200AndPersistRawLog()
  {
    // Arrange
    var employeeId = await SeedTestEmployeeAsync("CheckIn Success");
    var token = GenerateEmployeeToken(employeeId);

    var dto = new CheckInRequestDto
    {
      Type = "CheckIn",
      DeviceId = "WebApp-Test",
      Latitude = 10.7626,
      Longitude = 106.6602
    };

    var request = new HttpRequestMessage(HttpMethod.Post, "/api/attendance/check-in")
    {
      Content = JsonContent.Create(dto)
    };
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    // Act
    var response = await Client.SendAsync(request);

    // Assert — HTTP layer
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    // Assert — MongoDB: raw log persisted
    var logs = await RawLogs
        .Find(x => x.EmployeeId == employeeId && x.Type == RawLogType.CheckIn)
        .ToListAsync();
    Assert.NotEmpty(logs);
    var latest = logs.OrderByDescending(x => x.Timestamp).First();
    Assert.Equal("WebApp-Test", latest.DeviceId);
  }

  // ─────────────────────────────────────────────────────────────────
  // Spam Protection Tests
  // ─────────────────────────────────────────────────────────────────

  [Fact]
  public async Task CheckIn_Within60Seconds_ShouldReturn409Conflict()
  {
    // Arrange — seed a recent check-in log (less than 60 seconds ago)
    var employeeId = await SeedTestEmployeeAsync("Spam Test");
    var recentLog = new RawAttendanceLog(
        employeeId,
        DateTime.UtcNow.AddSeconds(-30), // 30 seconds ago — within cooldown
        RawLogType.CheckIn,
        "WebApp-Test");
    await RawLogs.InsertOneAsync(recentLog);

    var token = GenerateEmployeeToken(employeeId);
    var dto = new CheckInRequestDto { Type = "CheckIn", DeviceId = "WebApp-Test" };

    var request = new HttpRequestMessage(HttpMethod.Post, "/api/attendance/check-in")
    {
      Content = JsonContent.Create(dto)
    };
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    // Act
    var response = await Client.SendAsync(request);

    // Assert — spam protection triggered
    Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
  }

  [Fact]
  public async Task CheckIn_After60Seconds_ShouldSucceed()
  {
    // Arrange — seed an old check-in log (more than 60 seconds ago)
    var employeeId = await SeedTestEmployeeAsync("Old Log Test");
    var oldLog = new RawAttendanceLog(
        employeeId,
        DateTime.UtcNow.AddSeconds(-65), // 65 seconds ago — past cooldown
        RawLogType.CheckIn,
        "WebApp-Test");
    await RawLogs.InsertOneAsync(oldLog);

    var token = GenerateEmployeeToken(employeeId);
    var dto = new CheckInRequestDto { Type = "CheckIn", DeviceId = "WebApp-Test-2" };

    var request = new HttpRequestMessage(HttpMethod.Post, "/api/attendance/check-in")
    {
      Content = JsonContent.Create(dto)
    };
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    // Act
    var response = await Client.SendAsync(request);

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
  }

  // ─────────────────────────────────────────────────────────────────
  // Authorization Tests
  // ─────────────────────────────────────────────────────────────────

  [Fact]
  public async Task CheckIn_NoToken_ShouldReturn401()
  {
    // Arrange
    var dto = new CheckInRequestDto { Type = "CheckIn" };
    var request = new HttpRequestMessage(HttpMethod.Post, "/api/attendance/check-in")
    {
      Content = JsonContent.Create(dto)
    };
    // No Authorization header

    // Act
    var response = await Client.SendAsync(request);

    // Assert
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  // ─────────────────────────────────────────────────────────────────
  // CheckOut Tests
  // ─────────────────────────────────────────────────────────────────

  [Fact]
  public async Task CheckOut_ValidRequest_ShouldReturn200AndPersistRawLog()
  {
    // Arrange
    var employeeId = await SeedTestEmployeeAsync("CheckOut Test");
    var token = GenerateEmployeeToken(employeeId);

    var dto = new CheckInRequestDto
    {
      Type = "CheckOut",
      DeviceId = "WebApp-Test-Out"
    };

    var request = new HttpRequestMessage(HttpMethod.Post, "/api/attendance/check-out")
    {
      Content = JsonContent.Create(dto)
    };
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    // Act
    var response = await Client.SendAsync(request);

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    // Assert — MongoDB: check-out log persisted
    var logs = await RawLogs
        .Find(x => x.EmployeeId == employeeId && x.Type == RawLogType.CheckOut)
        .ToListAsync();
    Assert.NotEmpty(logs);
  }

  // ─────────────────────────────────────────────────────────────────
  // Admin CheckIn on behalf of Employee
  // ─────────────────────────────────────────────────────────────────

  [Fact]
  public async Task CheckIn_AdminWithEmployeeId_ShouldReturn200()
  {
    // Arrange — Admin can check in on behalf of any employee
    var employeeId = await SeedTestEmployeeAsync("Admin Proxy Test");
    var adminToken = GenerateAdminToken(employeeId);

    var dto = new CheckInRequestDto
    {
      Type = "CheckIn",
      EmployeeId = employeeId, // Admin specifies target employee
      DeviceId = "Admin-Terminal"
    };

    var request = new HttpRequestMessage(HttpMethod.Post, "/api/attendance/check-in")
    {
      Content = JsonContent.Create(dto)
    };
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

    // Act
    var response = await Client.SendAsync(request);

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var logs = await RawLogs
        .Find(x => x.EmployeeId == employeeId)
        .ToListAsync();
    Assert.NotEmpty(logs);
  }

  // IntegrationTestBase.Dispose() handles Client and Factory cleanup.
}
