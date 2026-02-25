using Xunit;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Employee.Application.Features.Leave.Commands.UpdateLeaveRequest;
using Employee.Application.Common.Interfaces.Organization.IRepository;
using Employee.Application.Common.Exceptions;
using Employee.Application.Features.Leave.Dtos;
using Employee.Domain.Entities.Leave;
using Employee.Domain.Enums;

namespace Employee.UnitTests.Features.Leave.Commands
{
  public class UpdateLeaveRequestHandlerTests
  {
    private readonly Mock<ILeaveRequestRepository> _mockRepo;
    private readonly UpdateLeaveRequestHandler _handler;

    public UpdateLeaveRequestHandlerTests()
    {
      _mockRepo = new Mock<ILeaveRequestRepository>();
      _handler = new UpdateLeaveRequestHandler(_mockRepo.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────

    private static LeaveRequest BuildPendingRequest(
        string id = "req-1",
        string empId = "emp-1",
        int daysFromNow = 5)
    {
      var from = DateTime.UtcNow.Date.AddDays(daysFromNow);
      var req = new LeaveRequest(empId, LeaveTypeEnum.Annual, from, from.AddDays(2), "Original reason");
      req.SetId(id);
      return req;
    }

    private static UpdateLeaveRequestCommand MakeCommand(
        string id = "req-1",
        string employeeId = "emp-1",
        string leaveType = "Annual",
        int daysFromNow = 7)
    {
      var from = GetNextMonday(daysFromNow);
      return new UpdateLeaveRequestCommand
      {
        Id = id,
        EmployeeId = employeeId,
        Dto = new UpdateLeaveRequestDto
        {
          Id = id,
          LeaveType = leaveType,
          FromDate = from,
          ToDate = from.AddDays(2),
          Reason = "Updated reason"
        }
      };
    }

    // ─── Not Found ────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenRequestNotFound_ShouldThrowNotFoundException()
    {
      _mockRepo.Setup(r => r.GetByIdAsync("missing", It.IsAny<CancellationToken>())).ReturnsAsync((LeaveRequest?)null);

      var command = new UpdateLeaveRequestCommand
      {
        Id = "missing",
        EmployeeId = "emp-1",
        Dto = new UpdateLeaveRequestDto
        {
          Id = "missing",
          LeaveType = "Annual",
          Reason = "Test",
          FromDate = DateTime.UtcNow.AddDays(10),
          ToDate = DateTime.UtcNow.AddDays(12)
        }
      };

      await Assert.ThrowsAsync<NotFoundException>(() =>
          _handler.Handle(command, CancellationToken.None));
    }

    // ─── Ownership Validation ─────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenEmployeeIdMismatch_ShouldThrowValidationException()
    {
      var existing = BuildPendingRequest(empId: "emp-1");
      _mockRepo.Setup(r => r.GetByIdAsync("req-1", It.IsAny<CancellationToken>())).ReturnsAsync(existing);

      // Different employee trying to edit
      var command = MakeCommand(employeeId: "emp-99");

      await Assert.ThrowsAsync<ValidationException>(() =>
          _handler.Handle(command, CancellationToken.None));
    }

    // ─── Invalid Leave Type ───────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenInvalidLeaveType_ShouldThrowValidationException()
    {
      var existing = BuildPendingRequest();
      _mockRepo.Setup(r => r.GetByIdAsync("req-1", It.IsAny<CancellationToken>())).ReturnsAsync(existing);

      var command = MakeCommand(leaveType: "NONEXISTENT_TYPE");

      await Assert.ThrowsAsync<ValidationException>(() =>
          _handler.Handle(command, CancellationToken.None));
    }

    // ─── Date Overlap ─────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenDateOverlapExists_ShouldThrowValidationException()
    {
      var existing = BuildPendingRequest();
      _mockRepo.Setup(r => r.GetByIdAsync("req-1", It.IsAny<CancellationToken>())).ReturnsAsync(existing);

      // ExistsOverlapAsync returns true — there's a competing request
      _mockRepo.Setup(r => r.ExistsOverlapAsync("emp-1", It.IsAny<DateTime>(), It.IsAny<DateTime>(), "req-1", It.IsAny<CancellationToken>()))
               .ReturnsAsync(true);

      var command = MakeCommand();

      await Assert.ThrowsAsync<ValidationException>(() =>
          _handler.Handle(command, CancellationToken.None));
    }

    // ─── Cannot Edit Non-Pending ──────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenRequestAlreadyApproved_ShouldThrowValidationException()
    {
      // Approve the request so the domain throws on Update()
      var existing = BuildPendingRequest();
      existing.Approve("manager-1", "OK");

      _mockRepo.Setup(r => r.GetByIdAsync("req-1", It.IsAny<CancellationToken>())).ReturnsAsync(existing);
      _mockRepo.Setup(r => r.ExistsOverlapAsync("emp-1", It.IsAny<DateTime>(), It.IsAny<DateTime>(), "req-1", It.IsAny<CancellationToken>()))
               .ReturnsAsync(false);

      var command = MakeCommand();

      // Handler wraps domain InvalidOperationException in ValidationException
      await Assert.ThrowsAsync<ValidationException>(() =>
          _handler.Handle(command, CancellationToken.None));
    }

    // ─── Happy Path ───────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenValidUpdate_ShouldCallUpdateAsyncOnce()
    {
      var existing = BuildPendingRequest();
      _mockRepo.Setup(r => r.GetByIdAsync("req-1", It.IsAny<CancellationToken>())).ReturnsAsync(existing);
      _mockRepo.Setup(r => r.ExistsOverlapAsync("emp-1", It.IsAny<DateTime>(), It.IsAny<DateTime>(), "req-1", It.IsAny<CancellationToken>()))
               .ReturnsAsync(false);
      _mockRepo.Setup(r => r.UpdateAsync("req-1", It.IsAny<LeaveRequest>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

      var command = MakeCommand();

      await _handler.Handle(command, CancellationToken.None); // Should not throw

      _mockRepo.Verify(r => r.UpdateAsync("req-1", It.IsAny<LeaveRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldPassExcludeIdToOverlapCheck_ToAvoidSelfConflict()
    {
      var existing = BuildPendingRequest(id: "req-42", empId: "emp-5");
      _mockRepo.Setup(r => r.GetByIdAsync("req-42", It.IsAny<CancellationToken>())).ReturnsAsync(existing);
      _mockRepo.Setup(r => r.ExistsOverlapAsync("emp-5", It.IsAny<DateTime>(), It.IsAny<DateTime>(), "req-42", It.IsAny<CancellationToken>()))
               .ReturnsAsync(false);
      _mockRepo.Setup(r => r.UpdateAsync("req-42", It.IsAny<LeaveRequest>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

      var from = GetNextMonday();
      var command = new UpdateLeaveRequestCommand
      {
        Id = "req-42",
        EmployeeId = "emp-5",
        Dto = new UpdateLeaveRequestDto
        {
          Id = "req-42",
          LeaveType = "Annual",
          FromDate = from,
          ToDate = from.AddDays(2),
          Reason = "Rescheduled"
        }
      };

      await _handler.Handle(command, CancellationToken.None);

      // Verify the excludeId "req-42" was passed to avoid self-conflict
      _mockRepo.Verify(r => r.ExistsOverlapAsync("emp-5", It.IsAny<DateTime>(), It.IsAny<DateTime>(), "req-42", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("Annual")]
    [InlineData("Sick")]
    [InlineData("Unpaid")]
    public async Task Handle_WithAllValidLeaveTypes_ShouldSucceed(string leaveType)
    {
      var existing = BuildPendingRequest();
      _mockRepo.Setup(r => r.GetByIdAsync("req-1", It.IsAny<CancellationToken>())).ReturnsAsync(existing);
      _mockRepo.Setup(r => r.ExistsOverlapAsync("emp-1", It.IsAny<DateTime>(), It.IsAny<DateTime>(), "req-1", It.IsAny<CancellationToken>()))
               .ReturnsAsync(false);
      _mockRepo.Setup(r => r.UpdateAsync("req-1", It.IsAny<LeaveRequest>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

      var command = MakeCommand(leaveType: leaveType);

      await _handler.Handle(command, CancellationToken.None); // Should not throw
    }

    // ─── Helper ───────────────────────────────────────────────────────────

    private static DateTime GetNextMonday(int atLeastDaysAhead = 7)
    {
      var d = DateTime.UtcNow.Date.AddDays(atLeastDaysAhead);
      while (d.DayOfWeek != DayOfWeek.Monday) d = d.AddDays(1);
      return d;
    }
  }
}
