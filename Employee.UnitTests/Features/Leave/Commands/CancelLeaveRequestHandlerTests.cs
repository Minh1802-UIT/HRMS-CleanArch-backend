using Xunit;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Employee.Application.Features.Leave.Commands.CancelLeaveRequest;
using Employee.Domain.Interfaces.Repositories;
using Employee.Application.Common.Interfaces.Organization.IService;
using Employee.Application.Common.Exceptions;
using Employee.Domain.Entities.Leave;
using Employee.Domain.Enums;

namespace Employee.UnitTests.Features.Leave.Commands
{
    public class CancelLeaveRequestHandlerTests
    {
        private readonly Mock<ILeaveRequestRepository> _mockRepo;
        private readonly Mock<ILeaveAllocationService> _mockAllocationService;
        private readonly Mock<ILeaveTypeRepository> _mockLeaveTypeRepo;
        private readonly CancelLeaveRequestHandler _handler;

        public CancelLeaveRequestHandlerTests()
        {
            _mockRepo = new Mock<ILeaveRequestRepository>();
            _mockAllocationService = new Mock<ILeaveAllocationService>();
            _mockLeaveTypeRepo = new Mock<ILeaveTypeRepository>();

            _handler = new CancelLeaveRequestHandler(
                _mockRepo.Object,
                _mockAllocationService.Object,
                _mockLeaveTypeRepo.Object,
                new Moq.Mock<Employee.Domain.Interfaces.Common.IDateTimeProvider>().Object
            );
        }

        private static LeaveRequest BuildPendingRequest(
            string empId = "emp-1",
            string id = "req-1",
            int fromDaysOffset = 5)
        {
            var request = new LeaveRequest(empId, LeaveCategory.Annual,
                DateTime.UtcNow.AddDays(fromDaysOffset),
                DateTime.UtcNow.AddDays(fromDaysOffset + 2),
                "Vacation");
            request.SetId(id);
            return request;
        }

        private static LeaveRequest BuildApprovedRequest(
            string empId = "emp-1",
            string id = "req-1",
            int fromDaysOffset = 5)
        {
            var request = BuildPendingRequest(empId, id, fromDaysOffset);
            request.Approve("manager-1", "OK");
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

            var command = new CancelLeaveRequestCommand("missing", "emp-1");

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WhenEmployeeIdMismatch_ShouldThrowValidationException()
        {
            var entity = BuildPendingRequest("emp-1");
            _mockRepo.Setup(r => r.GetByIdAsync("req-1", It.IsAny<CancellationToken>())).ReturnsAsync(entity);

            var command = new CancelLeaveRequestCommand("req-1", "emp-OTHER");

            await Assert.ThrowsAsync<ValidationException>(() =>
                _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WhenStatusIsRejected_ShouldThrowValidationException()
        {
            var entity = BuildPendingRequest();
            entity.Reject("manager-1", "Not approved");
            _mockRepo.Setup(r => r.GetByIdAsync("req-1", It.IsAny<CancellationToken>())).ReturnsAsync(entity);

            var command = new CancelLeaveRequestCommand("req-1", "emp-1");

            await Assert.ThrowsAsync<ValidationException>(() =>
                _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WhenStatusIsAlreadyCancelled_ShouldThrowValidationException()
        {
            var entity = BuildPendingRequest();
            entity.Cancel(System.DateTime.UtcNow);
            _mockRepo.Setup(r => r.GetByIdAsync("req-1", It.IsAny<CancellationToken>())).ReturnsAsync(entity);

            var command = new CancelLeaveRequestCommand("req-1", "emp-1");

            await Assert.ThrowsAsync<ValidationException>(() =>
                _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_CancelPendingRequest_ShouldSaveWithCancelledStatus()
        {
            var entity = BuildPendingRequest();
            _mockRepo.Setup(r => r.GetByIdAsync("req-1", It.IsAny<CancellationToken>())).ReturnsAsync(entity);

            var command = new CancelLeaveRequestCommand("req-1", "emp-1");

            await _handler.Handle(command, CancellationToken.None);

            _mockRepo.Verify(r => r.UpdateAsync("req-1", It.Is<LeaveRequest>(e =>
                e.Status == LeaveStatus.Cancelled), It.IsAny<CancellationToken>()), Times.Once);

            // No refund needed for pending
            _mockAllocationService.Verify(s => s.RefundDaysAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_CancelApprovedFutureRequest_ShouldRefundAllocationAndSave()
        {
            var entity = BuildApprovedRequest(fromDaysOffset: 5); // Future leave
            var leaveType = BuildLeaveType();

            _mockRepo.Setup(r => r.GetByIdAsync("req-1", It.IsAny<CancellationToken>())).ReturnsAsync(entity);
            _mockLeaveTypeRepo.Setup(r => r.GetByCodeAsync("Annual", It.IsAny<CancellationToken>())).ReturnsAsync(leaveType);
            _mockAllocationService.Setup(s => s.RefundDaysAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>()))
                .Returns(Task.CompletedTask);

            var command = new CancelLeaveRequestCommand("req-1", "emp-1");

            await _handler.Handle(command, CancellationToken.None);

            _mockAllocationService.Verify(s => s.RefundDaysAsync(
                "emp-1", "lt-1", entity.FromDate.Year.ToString(), It.IsAny<double>()),
                Times.Once);

            _mockRepo.Verify(r => r.UpdateAsync("req-1", It.Is<LeaveRequest>(e =>
                e.Status == LeaveStatus.Cancelled), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_CancelApprovedRequestAlreadyStarted_ShouldThrowValidationException()
        {
            // fromDaysOffset = -1 means the leave started yesterday
            var entity = BuildApprovedRequest(fromDaysOffset: -1);
            _mockRepo.Setup(r => r.GetByIdAsync("req-1", It.IsAny<CancellationToken>())).ReturnsAsync(entity);

            var command = new CancelLeaveRequestCommand("req-1", "emp-1");

            await Assert.ThrowsAsync<ValidationException>(() =>
                _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_CancelApprovedRequest_WhenLeaveTypeNotFound_ShouldThrowNotFoundException()
        {
            // LeaveType document missing → should throw to prevent cancelling without refund
            var entity = BuildApprovedRequest(fromDaysOffset: 5);
            _mockRepo.Setup(r => r.GetByIdAsync("req-1", It.IsAny<CancellationToken>())).ReturnsAsync(entity);
            _mockLeaveTypeRepo.Setup(r => r.GetByCodeAsync("Annual", It.IsAny<CancellationToken>())).ReturnsAsync((LeaveType?)null);

            var command = new CancelLeaveRequestCommand("req-1", "emp-1");

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _handler.Handle(command, CancellationToken.None));

            // Should NOT save — operation aborted before cancel
            _mockRepo.Verify(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<LeaveRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}

