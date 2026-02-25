using Xunit;
using Moq;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using Employee.Application.Features.Leave.Commands.ReviewLeaveRequest;
using Employee.Application.Common.Interfaces.Organization.IRepository;
using Employee.Application.Common.Interfaces.Organization.IService;
using Employee.Application.Common.Exceptions;
using Employee.Application.Features.Leave.Dtos;
using Employee.Domain.Entities.Leave;
using Employee.Domain.Enums;

namespace Employee.UnitTests.Features.Leave.Commands
{
  public class ReviewLeaveRequestHandlerTests
  {
    private readonly Mock<ILeaveRequestRepository> _mockRepo;
    private readonly Mock<ILeaveAllocationService> _mockAllocationService;
    private readonly Mock<ILeaveTypeRepository> _mockLeaveTypeRepo;
    private readonly Mock<IAuditLogService> _mockAuditService;
    private readonly Mock<IPublisher> _mockPublisher;
    private readonly ReviewLeaveRequestHandler _handler;

    public ReviewLeaveRequestHandlerTests()
    {
      _mockRepo = new Mock<ILeaveRequestRepository>();
      _mockAllocationService = new Mock<ILeaveAllocationService>();
      _mockLeaveTypeRepo = new Mock<ILeaveTypeRepository>();
      _mockAuditService = new Mock<IAuditLogService>();
      _mockPublisher = new Mock<IPublisher>();

      _handler = new ReviewLeaveRequestHandler(
          _mockRepo.Object,
          _mockAllocationService.Object,
          _mockLeaveTypeRepo.Object,
          _mockAuditService.Object,
          _mockPublisher.Object
      );
    }

    private static LeaveRequest BuildPendingRequest(string empId = "emp-1", string id = "req-1")
    {
      // FromDate/ToDate in the future so domain validation passes
      var request = new LeaveRequest(empId, LeaveTypeEnum.Annual,
          DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(3), "Vacation");
      request.SetId(id);
      return request;
    }

    private static LeaveType BuildLeaveType(string id = "lt-1", string code = "Annual")
    {
      var lt = new LeaveType($"{code} Leave", code, 12);
      lt.SetId(id);
      return lt;
    }

    [Fact]
    public async Task Handle_WhenRequestNotFound_ShouldThrowNotFoundException()
    {
      _mockRepo.Setup(r => r.GetByIdAsync("missing", It.IsAny<CancellationToken>())).ReturnsAsync((LeaveRequest?)null);

      var command = new ReviewLeaveRequestCommand
      {
        Id = "missing",
        ReviewDto = new ReviewLeaveRequestDto { Id = "missing", Status = "Approved" },
        ApprovedBy = "manager-1"
      };

      await Assert.ThrowsAsync<NotFoundException>(() =>
          _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenStatusIsInvalid_ShouldThrowValidationException()
    {
      var entity = BuildPendingRequest();
      _mockRepo.Setup(r => r.GetByIdAsync("req-1", It.IsAny<CancellationToken>())).ReturnsAsync(entity);

      var command = new ReviewLeaveRequestCommand
      {
        Id = "req-1",
        ReviewDto = new ReviewLeaveRequestDto { Id = "req-1", Status = "InvalidStatus" },
        ApprovedBy = "manager-1"
      };

      await Assert.ThrowsAsync<ValidationException>(() =>
          _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenStatusIsPending_ShouldThrowValidationException()
    {
      var entity = BuildPendingRequest();
      _mockRepo.Setup(r => r.GetByIdAsync("req-1", It.IsAny<CancellationToken>())).ReturnsAsync(entity);

      var command = new ReviewLeaveRequestCommand
      {
        Id = "req-1",
        ReviewDto = new ReviewLeaveRequestDto { Id = "req-1", Status = "Pending" },
        ApprovedBy = "manager-1"
      };

      await Assert.ThrowsAsync<ValidationException>(() =>
          _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ApproveRequest_ShouldDeductAllocationAndSave()
    {
      var entity = BuildPendingRequest();
      var leaveType = BuildLeaveType();

      _mockRepo.Setup(r => r.GetByIdAsync("req-1", It.IsAny<CancellationToken>())).ReturnsAsync(entity);
      _mockLeaveTypeRepo.Setup(r => r.GetByCodeAsync("Annual", It.IsAny<CancellationToken>())).ReturnsAsync(leaveType);
      _mockAllocationService.Setup(s => s.UpdateUsedDaysAsync(
          It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>()))
          .Returns(Task.CompletedTask);
      _mockAuditService.Setup(a => a.LogAsync(
          It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
          It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<object?>()))
          .Returns(Task.CompletedTask);

      var command = new ReviewLeaveRequestCommand
      {
        Id = "req-1",
        ReviewDto = new ReviewLeaveRequestDto { Id = "req-1", Status = "Approved", ManagerComment = "OK" },
        ApprovedBy = "manager-1"
      };

      await _handler.Handle(command, CancellationToken.None);

      _mockRepo.Verify(r => r.UpdateAsync("req-1", It.Is<LeaveRequest>(e =>
          e.Status == LeaveStatus.Approved), It.IsAny<CancellationToken>()), Times.Once);

      _mockAllocationService.Verify(s => s.UpdateUsedDaysAsync(
          "emp-1", "lt-1", entity.FromDate.Year.ToString(), It.IsAny<double>()), Times.Once);
    }

    [Fact]
    public async Task Handle_RejectRequest_ShouldNotDeductAllocation()
    {
      var entity = BuildPendingRequest();

      _mockRepo.Setup(r => r.GetByIdAsync("req-1", It.IsAny<CancellationToken>())).ReturnsAsync(entity);
      _mockAuditService.Setup(a => a.LogAsync(
          It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
          It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<object?>()))
          .Returns(Task.CompletedTask);

      var command = new ReviewLeaveRequestCommand
      {
        Id = "req-1",
        ReviewDto = new ReviewLeaveRequestDto { Id = "req-1", Status = "Rejected", ManagerComment = "Not approved" },
        ApprovedBy = "manager-1"
      };

      await _handler.Handle(command, CancellationToken.None);

      _mockRepo.Verify(r => r.UpdateAsync("req-1", It.Is<LeaveRequest>(e =>
          e.Status == LeaveStatus.Rejected), It.IsAny<CancellationToken>()), Times.Once);

      // Allocation should NOT be deducted for rejected requests
      _mockAllocationService.Verify(s => s.UpdateUsedDaysAsync(
          It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>()),
          Times.Never);
    }

    [Fact]
    public async Task Handle_ApproveRequest_WhenLeaveTypeNotFound_ShouldThrowNotFoundException()
    {
      var entity = BuildPendingRequest();

      _mockRepo.Setup(r => r.GetByIdAsync("req-1", It.IsAny<CancellationToken>())).ReturnsAsync(entity);
      _mockLeaveTypeRepo.Setup(r => r.GetByCodeAsync("Annual", It.IsAny<CancellationToken>())).ReturnsAsync((LeaveType?)null);

      var command = new ReviewLeaveRequestCommand
      {
        Id = "req-1",
        ReviewDto = new ReviewLeaveRequestDto { Id = "req-1", Status = "Approved" },
        ApprovedBy = "manager-1"
      };

      await Assert.ThrowsAsync<NotFoundException>(() =>
          _handler.Handle(command, CancellationToken.None));

      // Entity should NOT be saved — fail-fast before UpdateAsync
      _mockRepo.Verify(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<LeaveRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldAlwaysWriteAuditLog()
    {
      var entity = BuildPendingRequest();

      _mockRepo.Setup(r => r.GetByIdAsync("req-1", It.IsAny<CancellationToken>())).ReturnsAsync(entity);
      _mockAuditService.Setup(a => a.LogAsync(
          It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
          It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<object?>()))
          .Returns(Task.CompletedTask);

      var command = new ReviewLeaveRequestCommand
      {
        Id = "req-1",
        ReviewDto = new ReviewLeaveRequestDto { Id = "req-1", Status = "Rejected", ManagerComment = "No" },
        ApprovedBy = "manager-1"
      };

      await _handler.Handle(command, CancellationToken.None);

      _mockAuditService.Verify(a => a.LogAsync(
          "manager-1", It.IsAny<string>(), "REVIEW_LEAVE_REQUEST",
          "LeaveRequests", "req-1", It.IsAny<object?>(), It.IsAny<object?>()),
          Times.Once);
    }
  }
}
