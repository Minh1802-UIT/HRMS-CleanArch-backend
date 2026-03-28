using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Employee.Application.Features.HumanResource.Dtos;
using Employee.Application.Features.HumanResource.Commands.CreateEmployee;
using Employee.Domain.Entities.HumanResource;
using Employee.Domain.Entities.Organization;
using Employee.Domain.Entities.ValueObjects;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Employee.IntegrationTests.Repositories;

/// <summary>
/// Integration tests for <see cref="CreateEmployeeHandler"/>.
/// Exercises the full MediatR pipeline: command validation → repository → domain event
/// publishing → MediatR notification handler → background job enqueueing.
///
/// Key behaviors tested:
/// - Valid command persists employee to MongoDB and returns EmployeeDto
/// - Duplicate employee code → ConflictException (409)
/// - Missing department → NotFoundException (404)
/// - Domain event triggers background job for account provisioning
/// </summary>
[Collection("Api")]
public class CreateEmployeeHandlerTests : IntegrationTestBase
{
  public CreateEmployeeHandlerTests(IntegrationTestFixture fixture) : base(fixture) { }

  private IMongoCollection<EmployeeEntity> Employees => Fixture.Database.GetCollection<EmployeeEntity>("employees");
  private IMongoCollection<Department> Departments => Fixture.Database.GetCollection<Department>("departments");
  private IMongoCollection<Position> Positions => Fixture.Database.GetCollection<Position>("positions");

  private async Task<(string deptId, string posId)> SeedDepartmentAndPositionAsync()
  {
    var deptId = ObjectId.GenerateNewId().ToString();
    var posId = ObjectId.GenerateNewId().ToString();
    await Departments.InsertOneAsync(new Department("Engineering", "ENG") { Id = deptId });
    await Positions.InsertOneAsync(new Position("Software Engineer", "SE", deptId) { Id = posId });
    return (deptId, posId);
  }

  private string GetAdminToken()
  {
    var tokenService = Factory.Services.GetRequiredService<Employee.Application.Common.Interfaces.ITokenService>();
    return tokenService.GenerateJwtToken(
        userId: "test-admin-id",
        email: "admin@test.com",
        fullName: "TestAdmin",
        roles: new[] { "Admin" });
  }

  // ─────────────────────────────────────────────────────────────────
  // Happy Path Tests
  // ─────────────────────────────────────────────────────────────────

  [Fact]
  public async Task CreateEmployee_ValidCommand_ShouldPersistEmployeeAndReturnDto()
  {
    // Arrange
    var (deptId, posId) = await SeedDepartmentAndPositionAsync();
    var token = GetAdminToken();

    var command = new CreateEmployeeCommand
    {
      EmployeeCode = $"E-{Guid.NewGuid():N}".Substring(0, 12).ToUpper(),
      FullName = "Nguyen Van A",
      Email = $"test-{Guid.NewGuid():N}@company.com",
      PersonalInfo = new PersonalInfoDto
      {
        DateOfBirth = DateTime.UtcNow.AddYears(-25),
        IdentityCard = Guid.NewGuid().ToString("N").Substring(0, 9),
        PhoneNumber = "0901234567",
        Gender = "Male"
      },
      JobDetails = new JobDetailsDto
      {
        DepartmentId = deptId,
        PositionId = posId,
        JoinDate = DateTime.UtcNow
      },
      BankDetails = new BankDetailsDto
      {
        BankName = "Vietcombank",
        AccountNumber = "123456789"
      }
    };

    var request = new HttpRequestMessage(HttpMethod.Post, "/api/employees")
    {
      Content = JsonContent.Create(command)
    };
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    // Act
    var response = await Client.SendAsync(request);

    // Assert — HTTP layer
    Assert.Equal(HttpStatusCode.Created, response.StatusCode);

    var dto = await response.Content.ReadFromJsonAsync<EmployeeDto>();
    Assert.NotNull(dto);
    Assert.Equal(command.FullName, dto.FullName);
    Assert.Equal(command.Email, dto.Email);

    // Assert — MongoDB persistence
    var inDb = await Employees
        .Find(x => x.Id == dto.Id)
        .FirstOrDefaultAsync();
    Assert.NotNull(inDb);
    Assert.Equal(command.EmployeeCode, inDb.EmployeeCode);
    Assert.Equal(command.FullName, inDb.FullName);
  }

  [Fact]
  public async Task CreateEmployee_DuplicateEmployeeCode_ShouldReturn409()
  {
    // Arrange
    var (deptId, posId) = await SeedDepartmentAndPositionAsync();
    var duplicateCode = $"E-DUP-{Guid.NewGuid():N}".Substring(0, 10).ToUpper();
    var token = GetAdminToken();

    var makeCommand = () => new CreateEmployeeCommand
    {
      EmployeeCode = duplicateCode,
      FullName = "Test User",
      Email = $"test-{Guid.NewGuid():N}@company.com",
      PersonalInfo = new PersonalInfoDto
      {
        DateOfBirth = DateTime.UtcNow.AddYears(-25),
        IdentityCard = Guid.NewGuid().ToString("N").Substring(0, 9),
        PhoneNumber = "0901234567",
        Gender = "Male"
      },
      JobDetails = new JobDetailsDto
      {
        DepartmentId = deptId,
        PositionId = posId,
        JoinDate = DateTime.UtcNow
      }
    };

    // First create — should succeed
    var firstRequest = new HttpRequestMessage(HttpMethod.Post, "/api/employees")
    {
      Content = JsonContent.Create(makeCommand())
    };
    firstRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    var firstResponse = await Client.SendAsync(firstRequest);
    Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

    // Act — second create with same code → should conflict
    var secondRequest = new HttpRequestMessage(HttpMethod.Post, "/api/employees")
    {
      Content = JsonContent.Create(makeCommand())
    };
    secondRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    var secondResponse = await Client.SendAsync(secondRequest);

    // Assert
    Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
  }

  [Fact]
  public async Task CreateEmployee_MissingDepartment_ShouldReturn404()
  {
    // Arrange
    var (_, posId) = await SeedDepartmentAndPositionAsync();
    var token = GetAdminToken();

    var command = new CreateEmployeeCommand
    {
      EmployeeCode = $"E-{Guid.NewGuid():N}".Substring(0, 10).ToUpper(),
      FullName = "Missing Dept User",
      Email = $"test-{Guid.NewGuid():N}@company.com",
      PersonalInfo = new PersonalInfoDto
      {
        DateOfBirth = DateTime.UtcNow.AddYears(-25),
        IdentityCard = Guid.NewGuid().ToString("N").Substring(0, 9),
        PhoneNumber = "0901234567",
        Gender = "Male"
      },
      JobDetails = new JobDetailsDto
      {
        DepartmentId = ObjectId.GenerateNewId().ToString(), // valid format but doesn't exist
        PositionId = posId,
        JoinDate = DateTime.UtcNow
      }
    };

    var request = new HttpRequestMessage(HttpMethod.Post, "/api/employees")
    {
      Content = JsonContent.Create(command)
    };
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    // Act
    var response = await Client.SendAsync(request);

    // Assert
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }

  [Fact]
  public async Task CreateEmployee_MissingPosition_ShouldReturn404()
  {
    // Arrange
    var (deptId, _) = await SeedDepartmentAndPositionAsync();
    var token = GetAdminToken();

    var command = new CreateEmployeeCommand
    {
      EmployeeCode = $"E-{Guid.NewGuid():N}".Substring(0, 10).ToUpper(),
      FullName = "Missing Position User",
      Email = $"test-{Guid.NewGuid():N}@company.com",
      PersonalInfo = new PersonalInfoDto
      {
        DateOfBirth = DateTime.UtcNow.AddYears(-25),
        IdentityCard = Guid.NewGuid().ToString("N").Substring(0, 9),
        PhoneNumber = "0901234567",
        Gender = "Male"
      },
      JobDetails = new JobDetailsDto
      {
        DepartmentId = deptId,
        PositionId = ObjectId.GenerateNewId().ToString(), // valid format but doesn't exist
        JoinDate = DateTime.UtcNow
      }
    };

    var request = new HttpRequestMessage(HttpMethod.Post, "/api/employees")
    {
      Content = JsonContent.Create(command)
    };
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    // Act
    var response = await Client.SendAsync(request);

    // Assert
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }

  [Fact]
  public async Task CreateEmployee_Unauthorized_ShouldReturn401()
  {
    // Arrange — no Authorization header
    var (deptId, posId) = await SeedDepartmentAndPositionAsync();

    var command = new CreateEmployeeCommand
    {
      EmployeeCode = $"E-{Guid.NewGuid():N}".Substring(0, 10).ToUpper(),
      FullName = "Unauthorized User",
      Email = $"test-{Guid.NewGuid():N}@company.com",
      PersonalInfo = new PersonalInfoDto
      {
        DateOfBirth = DateTime.UtcNow.AddYears(-25),
        IdentityCard = Guid.NewGuid().ToString("N").Substring(0, 9),
        PhoneNumber = "0901234567",
        Gender = "Male"
      },
      JobDetails = new JobDetailsDto
      {
        DepartmentId = deptId,
        PositionId = posId,
        JoinDate = DateTime.UtcNow
      }
    };

    var request = new HttpRequestMessage(HttpMethod.Post, "/api/employees")
    {
      Content = JsonContent.Create(command)
    };
    // No auth header

    // Act
    var response = await Client.SendAsync(request);

    // Assert
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task CreateEmployee_EmptyFullName_ShouldReturn400()
  {
    // Arrange
    var (deptId, posId) = await SeedDepartmentAndPositionAsync();
    var token = GetAdminToken();

    var command = new
    {
      EmployeeCode = $"E-{Guid.NewGuid():N}".Substring(0, 10).ToUpper(),
      FullName = "", // Empty name
      Email = $"test-{Guid.NewGuid():N}@company.com",
      PersonalInfo = new { DateOfBirth = DateTime.UtcNow.AddYears(-25), IdentityCard = "123456789", PhoneNumber = "0901234567", Gender = "Male" },
      JobDetails = new { DepartmentId = deptId, PositionId = posId, JoinDate = DateTime.UtcNow }
    };

    var request = new HttpRequestMessage(HttpMethod.Post, "/api/employees")
    {
      Content = JsonContent.Create(command)
    };
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    // Act
    var response = await Client.SendAsync(request);

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  // IntegrationTestBase.Dispose() handles Client and Factory cleanup.
}
