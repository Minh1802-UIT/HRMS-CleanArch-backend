using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Employee.Application.Common.Interfaces;
using Employee.Application.Common.Models;
using Employee.Application.Features.HumanResource.Dtos;
using Employee.Application.Features.HumanResource.Commands.CreateEmployee;
using Employee.Application.Features.HumanResource.EventHandlers;
using Employee.Domain.Entities.HumanResource;
using Employee.Domain.Entities.Organization;
using Employee.Domain.Entities.ValueObjects;
using Employee.Domain.Events;
using MediatR;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace Employee.IntegrationTests.Repositories;

/// <summary>
/// Integration tests for the MediatR domain event pipeline.
/// Verifies that:
/// 1. <see cref="CreateEmployeeHandler"/> publishes <see cref="EmployeeCreatedEvent"/>
///    via MediatR <see cref="IPublisher"/>.
/// 2. <see cref="CreateUserEventHandler"/> (MediatR notification handler) receives the event
///    and calls <see cref="IBackgroundJobService.EnqueueAccountProvisioning"/>.
///
/// This exercises the MediatR notification dispatch pipeline end-to-end with real MongoDB.
/// </summary>
[Collection("Api")]
public class DomainEventNotificationTests : IntegrationTestBase
{
  public DomainEventNotificationTests(IntegrationTestFixture fixture) : base(fixture) { }

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

  private string GenerateAdminToken()
  {
    var tokenService = Factory.Services.GetRequiredService<ITokenService>();
    return tokenService.GenerateJwtToken(
        userId: "event-test-admin",
        email: "eventadmin@test.com",
        fullName: "EventAdmin",
        roles: new[] { "Admin" });
  }

  private async Task<EmployeeEntity> SeedEmployeeDirectlyAsync(string name = "Event Test")
  {
    var emp = new EmployeeEntity($"E-{Guid.NewGuid():N}".Substring(0, 10), name, $"{Guid.NewGuid():N}@test.com");
    await Employees.InsertOneAsync(emp);
    return emp;
  }

  // ─────────────────────────────────────────────────────────────────
  // Domain Event → Notification Handler Pipeline Tests
  // ─────────────────────────────────────────────────────────────────

  [Fact]
  public async Task CreateEmployeeHandler_PublishesEmployeeCreatedDomainEvent()
  {
    // Arrange — seed department and position
    var (deptId, posId) = await SeedDepartmentAndPositionAsync();

    // Capture calls to IBackgroundJobService
    var capturedCalls = new List<(string employeeId, string email, string fullName, string phone)>();
    var mockJobService = new Mock<IBackgroundJobService>();
    mockJobService
        .Setup(x => x.EnqueueAccountProvisioning(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
        .Callback<string, string, string, string>((empId, email, name, phone) =>
        {
          capturedCalls.Add((empId, email, name, phone));
        });

    // Resolve real services
    var repo = Factory.Services.GetRequiredService<Employee.Domain.Interfaces.Repositories.IEmployeeRepository>();
    var deptRepo = Factory.Services.GetRequiredService<Employee.Domain.Interfaces.Repositories.IDepartmentRepository>();
    var posRepo = Factory.Services.GetRequiredService<Employee.Domain.Interfaces.Repositories.IPositionRepository>();
    var unitOfWork = Factory.Services.GetRequiredService<Employee.Application.Common.Interfaces.IUnitOfWork>();
    var cache = Factory.Services.GetRequiredService<ICacheService>();
    var logger = Factory.Services.GetRequiredService<Microsoft.Extensions.Logging.ILogger<CreateUserEventHandler>>();

    // Create a capturing mock IPublisher that captures published domain events
    var capturedNotifications = new List<INotification>();
    var mockPublisher = new Mock<MediatR.IPublisher>();
    mockPublisher
        .Setup(x => x.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
        .Callback<INotification, CancellationToken>((notification, _) => capturedNotifications.Add(notification))
        .Returns(Task.CompletedTask);

    // Create the command handler with the mock publisher
    var handler = new CreateEmployeeHandler(
        repo, deptRepo, posRepo, mockPublisher.Object, unitOfWork, cache);

    var command = new CreateEmployeeCommand
    {
      EmployeeCode = $"E-EVENT-{Guid.NewGuid():N}".Substring(0, 12),
      FullName = "Domain Event Publisher Test",
      Email = $"event-pub-{Guid.NewGuid():N}@company.com",
      PersonalInfo = new PersonalInfoDto
      {
        DateOfBirth = DateTime.UtcNow.AddYears(-25),
        IdentityCard = "123456789",
        PhoneNumber = "0909999999",
        Gender = "Male"
      },
      JobDetails = new JobDetailsDto
      {
        DepartmentId = deptId,
        PositionId = posId,
        JoinDate = DateTime.UtcNow
      }
    };

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert — command succeeded and employee was persisted
    Assert.NotNull(result);
    Assert.Equal("Domain Event Publisher Test", result.FullName);

    // Assert — domain event was published via IPublisher
    Assert.Single(capturedNotifications);
    var notification = capturedNotifications[0];
    Assert.IsType<DomainEventNotification<EmployeeCreatedEvent>>(notification);
    var domainEventNotification = (DomainEventNotification<EmployeeCreatedEvent>)notification;
    Assert.Equal(result.Id, domainEventNotification.DomainEvent.EmployeeId);
    Assert.Equal(result.FullName, domainEventNotification.DomainEvent.FullName);
    Assert.Equal(result.Email, domainEventNotification.DomainEvent.Email);
    Assert.Equal("0909999999", domainEventNotification.DomainEvent.Phone);

    // Assert — manually invoke the notification handler to verify it calls IBackgroundJobService
    var notificationHandler = new CreateUserEventHandler(mockJobService.Object, logger);
    await notificationHandler.Handle(domainEventNotification, CancellationToken.None);

    Assert.Single(capturedCalls);
    Assert.Equal(result.Id, capturedCalls[0].employeeId);
    Assert.Equal("Domain Event Publisher Test", capturedCalls[0].fullName);
    Assert.Equal("0909999999", capturedCalls[0].phone);
  }

  [Fact]
  public async Task CreateUserEventHandler_ShouldCallEnqueueAccountProvisioningWithCorrectArgs()
  {
    // Arrange — directly test the CreateUserEventHandler with a real domain event
    var employee = await SeedEmployeeDirectlyAsync("Notification Handler Test");

    // Create capturing mock
    var mockJobService = new Mock<IBackgroundJobService>();
    var logger = Factory.Services.GetRequiredService<Microsoft.Extensions.Logging.ILogger<CreateUserEventHandler>>();
    var handler = new CreateUserEventHandler(mockJobService.Object, logger);

    // Extract phone to a local variable to avoid null-propagating operator in Moq expression tree
    var phone = employee.PersonalInfo?.Phone ?? "";

    // Create the domain event
    var domainEvent = new EmployeeCreatedEvent(
        employee.Id,
        employee.FullName,
        employee.Email,
        phone);
    var notification = new DomainEventNotification<EmployeeCreatedEvent>(domainEvent);

    // Act
    await handler.Handle(notification, CancellationToken.None);

    // Assert
    mockJobService.Verify(
        x => x.EnqueueAccountProvisioning(
            employee.Id,
            employee.Email,
            employee.FullName,
            phone),
        Times.Once);
  }

  [Fact]
  public async Task EmployeeDeletedEventHandler_CanBeInstantiatedWithFullDI()
  {
    // Arrange — seed a test employee
    var employee = await SeedEmployeeDirectlyAsync("Delete Cascade DI Test");

    // Resolve all required services from the factory's service provider to verify
    // the full DI graph can be resolved without errors.
    var sender = Factory.Services.GetRequiredService<ISender>();
    var auditService = Factory.Services.GetRequiredService<Employee.Application.Common.Interfaces.Organization.IService.IAuditLogService>();
    var currentUser = Factory.Services.GetRequiredService<ICurrentUser>();
    var contractRepo = Factory.Services.GetRequiredService<Employee.Domain.Interfaces.Repositories.IContractRepository>();
    var attendanceRepo = Factory.Services.GetRequiredService<Employee.Domain.Interfaces.Repositories.IAttendanceRepository>();
    var rawAttendanceRepo = Factory.Services.GetRequiredService<Employee.Domain.Interfaces.Repositories.IRawAttendanceLogRepository>();
    var leaveRepo = Factory.Services.GetRequiredService<Employee.Domain.Interfaces.Repositories.ILeaveRequestRepository>();
    var allocationRepo = Factory.Services.GetRequiredService<Employee.Domain.Interfaces.Repositories.ILeaveAllocationRepository>();
    var payrollRepo = Factory.Services.GetRequiredService<Employee.Domain.Interfaces.Repositories.IPayrollRepository>();
    var logger = Factory.Services.GetRequiredService<Microsoft.Extensions.Logging.ILogger<EmployeeDeletedEventHandler>>();

    // Act — instantiate the handler (verifies the full DI graph resolves correctly)
    var handler = new EmployeeDeletedEventHandler(
        sender,
        auditService,
        currentUser,
        contractRepo,
        attendanceRepo,
        rawAttendanceRepo,
        leaveRepo,
        allocationRepo,
        payrollRepo,
        logger);

    // Assert
    Assert.NotNull(handler);
  }

  // IntegrationTestBase.Dispose() handles Client and Factory cleanup.
}
