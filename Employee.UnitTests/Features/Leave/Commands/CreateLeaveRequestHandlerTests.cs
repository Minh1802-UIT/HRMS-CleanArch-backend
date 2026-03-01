using Xunit;
using Moq;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using Employee.Application.Features.Leave.Commands.CreateLeaveRequest;
using Employee.Domain.Interfaces.Repositories;
using Employee.Application.Common.Interfaces.Organization.IService;
using Employee.Application.Common.Exceptions;
using Employee.Application.Features.Leave.Dtos;
using Employee.Domain.Entities.Leave;
using Employee.Domain.Enums;
using EmployeeEntity = Employee.Domain.Entities.HumanResource.EmployeeEntity;

namespace Employee.UnitTests.Features.Leave.Commands
{
  public class CreateLeaveRequestHandlerTests
  {
    private readonly Mock<ILeaveRequestRepository> _mockRepo;
    private readonly Mock<IEmployeeRepository> _mockEmpRepo;
    private readonly Mock<ILeaveAllocationService> _mockAllocationService;
    private readonly Mock<ILeaveTypeRepository> _mockLeaveTypeRepo;
    private readonly Mock<IPublisher> _mockPublisher;
    private readonly CreateLeaveRequestHandler _handler;

    public CreateLeaveRequestHandlerTests()
    {
      _mockRepo = new Mock<ILeaveRequestRepository>();
      _mockEmpRepo = new Mock<IEmployeeRepository>();
      _mockAllocationService = new Mock<ILeaveAllocationService>();
      _mockLeaveTypeRepo = new Mock<ILeaveTypeRepository>();
      _mockPublisher = new Mock<IPublisher>();

      _handler = new CreateLeaveRequestHandler(
          _mockRepo.Object,
          _mockEmpRepo.Object,
          _mockAllocationService.Object,
          _mockLeaveTypeRepo.Object,
          _mockPublisher.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────

    private static LeaveType BuildLeaveType(string id = "lt-1", string code = "Annual")
    {
      var lt = new LeaveType($"{code} Leave", code, 12);
      lt.SetId(id);
      return lt;
    }

    private static LeaveAllocationDto BuildAllocation(double remaining = 10)
        => new LeaveAllocationDto
        {
          EmployeeId = "emp-1",
          LeaveTypeId = "lt-1",
          Year = DateTime.UtcNow.Year.ToString(),
          TotalDays = 12,
          UsedDays = 12 - remaining,
          RemainingDays = (int)remaining
        };

    private static EmployeeEntity BuildEmployee(string id = "emp-1")
    {
      var emp = new EmployeeEntity("EMP-001", "John Doe", "john@test.com");
      emp.SetId(id);
      return emp;
    }

    /// <summary>
    /// Creates a command with dates that have at least 3 working days.
    /// The monday-to-wednesday span guaranteed to include 3 working days.
    /// </summary>
    private static CreateLeaveRequestCommand MakeCommand(
        string leaveType = "Annual",
        string employeeId = "emp-1",
        int daysFromNow = 5,
        int spanDays = 2)
    {
      // Move to a Wednesday so the span is Mon-Wed (3 working days)
      var from = DateTime.UtcNow.Date.AddDays(daysFromNow);
      var to = from.AddDays(spanDays);
      return new CreateLeaveRequestCommand
      {
        LeaveType = leaveType,
        EmployeeId = employeeId,
        FromDate = from,
        ToDate = to,
        Reason = "Unit test"
      };
    }

    // ─── Validation Tests ─────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenLeaveCategoryNameNotFoundInDb_ShouldThrowNotFoundException()
    {
      // The command uses an enum-parseable name ("Annual"), but the DB has nothing for that code
      _mockLeaveTypeRepo.Setup(r => r.GetByCodeAsync("Annual", It.IsAny<CancellationToken>())).ReturnsAsync((LeaveType?)null);

      var command = MakeCommand("Annual");

      await Assert.ThrowsAsync<NotFoundException>(() =>
          _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenLeaveTypeIdNotFoundInDb_ShouldThrowNotFoundException()
    {
      // The command uses an ID that doesn't parse as an enum → treated as document ID
      _mockLeaveTypeRepo.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((LeaveType?)null);
      _mockLeaveTypeRepo.Setup(r => r.GetByIdAsync("non-existent-id", It.IsAny<CancellationToken>())).ReturnsAsync((LeaveType?)null);

      var command = MakeCommand("non-existent-id");

      await Assert.ThrowsAsync<NotFoundException>(() =>
          _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenLeaveTypeIdHasInvalidCode_ShouldThrowValidationException()
    {
      // The leave type doc exists but its Code is not a valid LeaveCategory value
      var lt = BuildLeaveType("lt-99", "INVALID_CODE");
      _mockLeaveTypeRepo.Setup(r => r.GetByIdAsync("lt-99", It.IsAny<CancellationToken>())).ReturnsAsync(lt);

      var command = MakeCommand("lt-99");

      await Assert.ThrowsAsync<ValidationException>(() =>
          _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenDateOverlapExists_ShouldThrowConflictException()
    {
      var leaveType = BuildLeaveType();
      _mockLeaveTypeRepo.Setup(r => r.GetByCodeAsync("Annual", It.IsAny<CancellationToken>())).ReturnsAsync(leaveType);
      _mockRepo.Setup(r => r.ExistsOverlapAsync("emp-1", It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, It.IsAny<CancellationToken>()))
               .ReturnsAsync(true);

      var command = MakeCommand();

      await Assert.ThrowsAsync<ConflictException>(() =>
          _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenBalanceInsufficient_ShouldThrowValidationException()
    {
      var leaveType = BuildLeaveType();
      _mockLeaveTypeRepo.Setup(r => r.GetByCodeAsync("Annual", It.IsAny<CancellationToken>())).ReturnsAsync(leaveType);
      _mockRepo.Setup(r => r.ExistsOverlapAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, It.IsAny<CancellationToken>()))
               .ReturnsAsync(false);

      // Span: 2 working days but only 1 remaining
      var allocation = BuildAllocation(remaining: 1);
      _mockAllocationService.Setup(s => s.GetByEmployeeAndTypeAsync("emp-1", "lt-1", It.IsAny<string>()))
                            .ReturnsAsync(allocation);

      // Use a Mon-Wed span to guarantee > 1 working days
      var from = GetNextMonday();
      var command = new CreateLeaveRequestCommand
      {
        LeaveType = "Annual",
        EmployeeId = "emp-1",
        FromDate = from,
        ToDate = from.AddDays(2), // 3 working days
        Reason = "Vacation"
      };

      await Assert.ThrowsAsync<ValidationException>(() =>
          _handler.Handle(command, CancellationToken.None));
    }

    // ─── Happy Path Tests ─────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenLeaveTypePassedAsEnumName_ShouldCreateAndReturnDto()
    {
      var leaveType = BuildLeaveType();
      _mockLeaveTypeRepo.Setup(r => r.GetByCodeAsync("Annual", It.IsAny<CancellationToken>())).ReturnsAsync(leaveType);
      _mockRepo.Setup(r => r.ExistsOverlapAsync("emp-1", It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, It.IsAny<CancellationToken>()))
               .ReturnsAsync(false);

      var allocation = BuildAllocation(remaining: 10);
      _mockAllocationService.Setup(s => s.GetByEmployeeAndTypeAsync("emp-1", "lt-1", It.IsAny<string>()))
                            .ReturnsAsync(allocation);

      _mockRepo.Setup(r => r.CreateAsync(It.IsAny<LeaveRequest>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

      var employee = BuildEmployee();
      _mockEmpRepo.Setup(r => r.GetByIdAsync("emp-1", It.IsAny<CancellationToken>())).ReturnsAsync(employee);

      var command = MakeCommand("Annual");

      var result = await _handler.Handle(command, CancellationToken.None);

      Assert.NotNull(result);
      Assert.Equal("emp-1", result.EmployeeId);
      Assert.Equal("Pending", result.Status);
      _mockRepo.Verify(r => r.CreateAsync(It.IsAny<LeaveRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenLeaveTypePassedAsDocumentId_ShouldResolveAndCreate()
    {
      // "lt-1" doesn't parse as an enum name, so handler treats it as a document ID
      var leaveType = BuildLeaveType("lt-1", "Annual");
      _mockLeaveTypeRepo.Setup(r => r.GetByIdAsync("lt-1", It.IsAny<CancellationToken>())).ReturnsAsync(leaveType);
      _mockRepo.Setup(r => r.ExistsOverlapAsync("emp-1", It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, It.IsAny<CancellationToken>()))
               .ReturnsAsync(false);

      var allocation = BuildAllocation(remaining: 10);
      _mockAllocationService.Setup(s => s.GetByEmployeeAndTypeAsync("emp-1", "lt-1", It.IsAny<string>()))
                            .ReturnsAsync(allocation);

      _mockRepo.Setup(r => r.CreateAsync(It.IsAny<LeaveRequest>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

      var employee = BuildEmployee();
      _mockEmpRepo.Setup(r => r.GetByIdAsync("emp-1", It.IsAny<CancellationToken>())).ReturnsAsync(employee);

      var command = MakeCommand("lt-1");

      var result = await _handler.Handle(command, CancellationToken.None);

      Assert.NotNull(result);
      Assert.Equal("Pending", result.Status);
    }

    [Fact]
    public async Task Handle_WhenEmployeeNotFound_ShouldStillReturnDtoWithUnknownName()
    {
      var leaveType = BuildLeaveType();
      _mockLeaveTypeRepo.Setup(r => r.GetByCodeAsync("Annual", It.IsAny<CancellationToken>())).ReturnsAsync(leaveType);
      _mockRepo.Setup(r => r.ExistsOverlapAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, It.IsAny<CancellationToken>()))
               .ReturnsAsync(false);

      var allocation = BuildAllocation(remaining: 10);
      _mockAllocationService.Setup(s => s.GetByEmployeeAndTypeAsync("emp-1", "lt-1", It.IsAny<string>()))
                            .ReturnsAsync(allocation);

      _mockRepo.Setup(r => r.CreateAsync(It.IsAny<LeaveRequest>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
      _mockEmpRepo.Setup(r => r.GetByIdAsync("emp-1", It.IsAny<CancellationToken>())).ReturnsAsync((EmployeeEntity?)null);

      var command = MakeCommand();

      var result = await _handler.Handle(command, CancellationToken.None);

      Assert.NotNull(result);
      Assert.Equal("Unknown", result.EmployeeName);
    }

    [Fact]
    public async Task Handle_WhenNullAllocation_ShouldTreatBalanceAsZeroAndThrowIfDaysRequested()
    {
      var leaveType = BuildLeaveType();
      _mockLeaveTypeRepo.Setup(r => r.GetByCodeAsync("Annual", It.IsAny<CancellationToken>())).ReturnsAsync(leaveType);
      _mockRepo.Setup(r => r.ExistsOverlapAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, It.IsAny<CancellationToken>()))
               .ReturnsAsync(false);

      // Null allocation → balance = 0 → any request with working days will fail
      _mockAllocationService.Setup(s => s.GetByEmployeeAndTypeAsync("emp-1", "lt-1", It.IsAny<string>()))
                            .ReturnsAsync((LeaveAllocationDto?)null);

      var from = GetNextMonday();
      var command = new CreateLeaveRequestCommand
      {
        LeaveType = "Annual",
        EmployeeId = "emp-1",
        FromDate = from,
        ToDate = from,    // 1 working day
        Reason = "Test"
      };

      await Assert.ThrowsAsync<ValidationException>(() =>
          _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldNotDeductBalance_BalanceDeductionUpdatedAfterApproval()
    {
      // Balance deduction must NOT happen in CreateLeaveRequestHandler — only in Review
      var leaveType = BuildLeaveType();
      _mockLeaveTypeRepo.Setup(r => r.GetByCodeAsync("Annual", It.IsAny<CancellationToken>())).ReturnsAsync(leaveType);
      _mockRepo.Setup(r => r.ExistsOverlapAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, It.IsAny<CancellationToken>()))
               .ReturnsAsync(false);

      var allocation = BuildAllocation(remaining: 10);
      _mockAllocationService.Setup(s => s.GetByEmployeeAndTypeAsync("emp-1", "lt-1", It.IsAny<string>()))
                            .ReturnsAsync(allocation);

      _mockRepo.Setup(r => r.CreateAsync(It.IsAny<LeaveRequest>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
      _mockEmpRepo.Setup(r => r.GetByIdAsync("emp-1", It.IsAny<CancellationToken>())).ReturnsAsync(BuildEmployee());

      await _handler.Handle(MakeCommand(), CancellationToken.None);

      // UpdateUsedDaysAsync must NOT have been called during create
      _mockAllocationService.Verify(
          s => s.UpdateUsedDaysAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>()),
          Times.Never);
    }

    // ─── Helper ───────────────────────────────────────────────────────────

    private static DateTime GetNextMonday()
    {
      var d = DateTime.UtcNow.Date.AddDays(7);
      while (d.DayOfWeek != DayOfWeek.Monday) d = d.AddDays(1);
      return d;
    }
  }
}
