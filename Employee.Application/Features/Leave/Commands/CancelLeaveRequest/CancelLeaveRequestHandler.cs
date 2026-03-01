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
            if (entity == null) throw new NotFoundException("Không tìm thấy đơn");

            if (entity.EmployeeId != request.EmployeeId)
                throw new ValidationException("Bạn không có quyền hủy đơn này");

            // Allow canceling Approved requests now (with Refund logic)
            if (entity.Status != Employee.Domain.Enums.LeaveStatus.Pending &&
                entity.Status != Employee.Domain.Enums.LeaveStatus.Approved)
            {
                throw new ValidationException("Chỉ được hủy đơn khi đang chờ duyệt hoặc đã duyệt (nhưng chưa nghỉ).");
            }

            // Refund logic if cancelling an Approved leave
            if (entity.Status == Employee.Domain.Enums.LeaveStatus.Approved)
            {
                // Guard: only allow cancel if leave hasn't started yet
                // Dùng UTC+7 (Asia/Ho_Chi_Minh) nhất quán với AttendanceProcessingService
                var vnOffset = TimeSpan.FromHours(7);
                var todayVn = DateTimeOffset.UtcNow.ToOffset(vnOffset).Date;
                if (entity.FromDate.Date <= todayVn)
                    throw new ValidationException("Không thể hủy đơn nghỉ phép đã bắt đầu hoặc đã qua.");

                // Resolve LeaveType document ID — fail fast to avoid cancelling without refund
                var leaveTypeDoc = await _leaveTypeRepo.GetByCodeAsync(entity.LeaveType.ToString(), cancellationToken);
                if (leaveTypeDoc == null)
                    throw new NotFoundException($"Không tìm thấy loại phép '{entity.LeaveType}' trong hệ thống. Không thể hoàn trả ngày phép.");

                var year = entity.FromDate.Year.ToString();
                // Dùng cùng logic với ReviewLeaveRequestHandler: Sandwich Rule → ngày lịch, ngược lại → ngày làm việc
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

