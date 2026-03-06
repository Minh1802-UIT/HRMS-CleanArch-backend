using Employee.Application.Common.Exceptions;
using Employee.Domain.Interfaces.Repositories;
using Employee.Application.Common.Interfaces.Organization.IService;
using MediatR;

namespace Employee.Application.Features.Leave.Commands.CancelLeaveRequest
{
    public class CancelLeaveRequestHandler : IRequestHandler<CancelLeaveRequestCommand>
    {
        private readonly ILeaveRequestRepository _repo;
        private readonly ILeaveAllocationService _allocationService;
        private readonly ILeaveTypeRepository _leaveTypeRepo;
        private readonly Employee.Domain.Interfaces.Common.IDateTimeProvider _dateTime;

        public CancelLeaveRequestHandler(
            ILeaveRequestRepository repo,
            ILeaveAllocationService allocationService,
            ILeaveTypeRepository leaveTypeRepo,
            Employee.Domain.Interfaces.Common.IDateTimeProvider dateTime)
        {
            _repo = repo;
            _allocationService = allocationService;
            _leaveTypeRepo = leaveTypeRepo;
            _dateTime = dateTime;
        }

        public async Task Handle(CancelLeaveRequestCommand request, CancellationToken cancellationToken)
        {
            var entity = await _repo.GetByIdAsync(request.Id, cancellationToken);
            if (entity == null) throw new NotFoundException("Leave request not found.");

            if (entity.EmployeeId != request.EmployeeId)
                throw new ValidationException("You do not have permission to cancel this leave request.");

            // Allow canceling Approved requests (with refund logic)
            if (entity.Status != Employee.Domain.Enums.LeaveStatus.Pending &&
                entity.Status != Employee.Domain.Enums.LeaveStatus.Approved)
            {
                throw new ValidationException("Leave request can only be cancelled when it is Pending or Approved (but not yet started).");
            }

            // Refund logic if cancelling an Approved leave
            if (entity.Status == Employee.Domain.Enums.LeaveStatus.Approved)
            {
                // Guard: only allow cancel if leave hasn't started yet
                // Use Vietnam time (UTC+7) consistent with AttendanceProcessingService
                var vnOffset = TimeSpan.FromHours(7);
                var todayVn = DateTimeOffset.UtcNow.ToOffset(vnOffset).Date;
                if (entity.FromDate.Date <= todayVn)
                    throw new ValidationException("Cannot cancel a leave request that has already started or passed.");

                // Resolve LeaveType document ID — fail fast to avoid cancelling without refund
                var leaveTypeDoc = await _leaveTypeRepo.GetByCodeAsync(entity.LeaveType.ToString(), cancellationToken);
                if (leaveTypeDoc == null)
                    throw new NotFoundException($"Leave type '{entity.LeaveType}' not found in the system. Cannot refund leave days.");

                var year = entity.FromDate.Year.ToString();
                // Apply Sandwich Rule consistently with ReviewLeaveRequestHandler: calendar days vs working days
                var days = leaveTypeDoc.IsSandwichRuleApplied
                    ? Employee.Application.Common.Utils.DateHelper.CountCalendarDays(entity.FromDate, entity.ToDate)
                    : Employee.Application.Common.Utils.DateHelper.CountWorkingDays(entity.FromDate, entity.ToDate);
                await _allocationService.RefundDaysAsync(entity.EmployeeId, leaveTypeDoc.Id, year, days);
            }

            // Use domain Cancel() method instead of soft-delete to preserve audit trail
            entity.Cancel(_dateTime.UtcNow);
            await _repo.UpdateAsync(request.Id, entity, cancellationToken);
        }
    }
}

