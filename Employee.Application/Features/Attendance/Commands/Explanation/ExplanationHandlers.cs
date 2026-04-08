using Employee.Application.Features.Attendance.Dtos;
using Employee.Application.Features.Attendance.Mappers;
using Employee.Application.Common.Interfaces;
using Employee.Application.Common.Exceptions;
using Employee.Domain.Entities.Attendance;
using Employee.Domain.Interfaces.Repositories;
using Employee.Domain.Enums;
using MediatR;

namespace Employee.Application.Features.Attendance.Commands.Explanation
{
  // ── SUBMIT ────────────────────────────────────────────────────────────────

  public class SubmitExplanationCommand : IRequest<AttendanceExplanationDto>
  {
    public string EmployeeId { get; set; } = string.Empty;
    public SubmitExplanationDto Dto { get; set; } = null!;
  }

  public class SubmitExplanationHandler : IRequestHandler<SubmitExplanationCommand, AttendanceExplanationDto>
  {
    private readonly IAttendanceExplanationRepository _repo;
    private readonly IAttendanceRepository _attendanceRepo;
    private readonly ICurrentUser _currentUser;
    private readonly IShiftRepository _shiftRepo;
    private readonly IEmployeeRepository _employeeRepo;

    public SubmitExplanationHandler(
        IAttendanceExplanationRepository repo,
        IAttendanceRepository attendanceRepo,
        ICurrentUser currentUser,
        IShiftRepository shiftRepo,
        IEmployeeRepository employeeRepo)
    {
      _repo = repo;
      _attendanceRepo = attendanceRepo;
      _currentUser = currentUser;
      _shiftRepo = shiftRepo;
      _employeeRepo = employeeRepo;
    }

    public async Task<AttendanceExplanationDto> Handle(SubmitExplanationCommand request, CancellationToken cancellationToken)
    {
      // Ownership check: only the employee or HR/Admin can submit an explanation for a given employeeId
      var currentEmployeeId = _currentUser.EmployeeId;
      var currentUserId = _currentUser.UserId;
      var isOwner = request.EmployeeId == currentEmployeeId || request.EmployeeId == currentUserId;
      var isHrOrAdmin = _currentUser.IsInRole("HR") || _currentUser.IsInRole("Admin");

      if (!isOwner && !isHrOrAdmin)
        throw new ForbiddenException(
            "You do not have permission to add an attendance explanation for this employee.");

      if (string.IsNullOrWhiteSpace(request.Dto.Reason))
        throw new ValidationException("Reason is required.");

      // Validate that the work-date actually has IsMissingPunch or IsMissingCheckIn = true
      var monthKey = request.Dto.WorkDate.ToString("MM-yyyy");
      var bucket = await _attendanceRepo.GetByEmployeeAndMonthAsync(request.EmployeeId, monthKey);
      var dailyLog = bucket?.DailyLogs.FirstOrDefault(l => l.Date.Date == request.Dto.WorkDate.Date);

      if (dailyLog == null)
        throw new NotFoundException($"Không tìm thấy ngày công {request.Dto.WorkDate:dd/MM/yyyy}.");

      var type = (ExplanationType)request.Dto.Type;

      if (type == ExplanationType.CompensatoryTime)
      {
         // Calculate the actual deficit: how many hours short of standard this day is
         var shift = await GetEffectiveShiftForSubmitAsync(request.EmployeeId, request.Dto.WorkDate);
         double standardHours = shift?.StandardWorkingHours ?? 8.0;
         double deficit = Math.Max(0, standardHours - dailyLog.WorkingHours);

         if (deficit <= 0)
             throw new ValidationException($"Ngày này đã đủ {standardHours}h công — không cần bù giờ.");

         // Auto-cap requested hours to the deficit
         double cappedHours = Math.Min(request.Dto.RequestedCompHours, deficit);
         if (cappedHours <= 0)
             throw new ValidationException("Số giờ bù phải lớn hơn 0.");

         if (bucket!.AvailableCompensatoryHours < cappedHours)
             throw new ValidationException($"Không đủ giờ dư. Số giờ khả dụng: {bucket.AvailableCompensatoryHours:F1}h, cần bù: {cappedHours:F1}h");

         // Override the requested hours with the capped value
         request.Dto.RequestedCompHours = cappedHours;

         bucket.ReserveCompensatoryHours(cappedHours);
         await _attendanceRepo.UpdateAsync(bucket.Id, bucket, cancellationToken);
      }
      else
      {
          // Accept explanation for missing punch
          bool isMissingCheckout = dailyLog.CheckIn.HasValue && !dailyLog.CheckOut.HasValue;
          if (!dailyLog.IsMissingPunch && !dailyLog.IsMissingCheckIn && !isMissingCheckout)
            throw new ConflictException("Ngày này không có trường hợp cần giải trình.");
      }

      // Prevent duplicate pending submission
      var existing = await _repo.GetByEmployeeAndDateAsync(request.EmployeeId, request.Dto.WorkDate, cancellationToken);
      if (existing != null && existing.Status == Domain.Enums.ExplanationStatus.Pending)
        throw new ConflictException("Bạn đã có đơn giải trình đang chờ duyệt cho ngày này.");

      var explanation = new AttendanceExplanation(
          request.EmployeeId, request.Dto.WorkDate, request.Dto.Reason,
          type, request.Dto.RequestedCompHours);
      await _repo.CreateAsync(explanation);

      return explanation.ToDto(employeeName: null);
    }

    private async Task<Domain.Entities.Attendance.Shift?> GetEffectiveShiftForSubmitAsync(string employeeId, DateTime date)
    {
      var rosterShift = await _shiftRepo.GetShiftByDateAsync(employeeId, date);
      if (rosterShift != null) return rosterShift;

      var employee = await _employeeRepo.GetByIdAsync(employeeId);
      if (!string.IsNullOrEmpty(employee?.JobDetails.ShiftId))
        return await _shiftRepo.GetByIdAsync(employee.JobDetails.ShiftId);

      return null;
    }
  }

  // ── REVIEW (APPROVE / REJECT) ─────────────────────────────────────────────

  public class ReviewExplanationCommand : IRequest<AttendanceExplanationDto>
  {
    public string ExplanationId { get; set; } = string.Empty;
    public string ReviewerUserId { get; set; } = string.Empty;
    public ReviewExplanationDto Dto { get; set; } = null!;
  }

  public class ReviewExplanationHandler : IRequestHandler<ReviewExplanationCommand, AttendanceExplanationDto>
  {
    private readonly IAttendanceExplanationRepository _repo;
    private readonly IAttendanceRepository _attendanceRepo;
    private readonly IShiftRepository _shiftRepo;
    private readonly IEmployeeRepository _employeeRepo;

    public ReviewExplanationHandler(
        IAttendanceExplanationRepository repo,
        IAttendanceRepository attendanceRepo,
        IShiftRepository shiftRepo,
        IEmployeeRepository employeeRepo)
    {
      _repo = repo;
      _attendanceRepo = attendanceRepo;
      _shiftRepo = shiftRepo;
      _employeeRepo = employeeRepo;
    }

    public async Task<AttendanceExplanationDto> Handle(ReviewExplanationCommand request, CancellationToken cancellationToken)
    {
      var explanation = await _repo.GetByIdAsync(request.ExplanationId, cancellationToken)
          ?? throw new NotFoundException($"Không tìm thấy giải trình với Id={request.ExplanationId}.");

      var action = request.Dto.Action?.ToLower();
      if (action != "approve" && action != "reject")
        throw new ValidationException("Action phải là 'Approve' hoặc 'Reject'.");

      if (action == "approve")
      {
        explanation.Approve(request.ReviewerUserId, request.Dto.Note);
        await ApproveAttendanceAsync(explanation, cancellationToken);
      }
      else
      {
        explanation.Reject(request.ReviewerUserId, request.Dto.Note ?? string.Empty);
        if (explanation.Type == ExplanationType.CompensatoryTime)
        {
           var monthKey = explanation.WorkDate.ToString("MM-yyyy");
           var bucket = await _attendanceRepo.GetByEmployeeAndMonthAsync(explanation.EmployeeId, monthKey);
           if (bucket != null)
           {
               bucket.CancelCompensatoryHours(explanation.RequestedCompHours);
               await _attendanceRepo.UpdateAsync(bucket.Id, bucket, cancellationToken);
           }
        }
      }

      await _repo.UpdateAsync(explanation.Id, explanation, cancellationToken);

      return explanation.ToDto(employeeName: null);
    }

    // When approved: set WorkingHours = shift standard hours, Status = Present, clear IsMissingPunch
    private async Task ApproveAttendanceAsync(AttendanceExplanation explanation, CancellationToken cancellationToken)
    {
      var monthKey = explanation.WorkDate.ToString("MM-yyyy");
      var bucket = await _attendanceRepo.GetByEmployeeAndMonthAsync(explanation.EmployeeId, monthKey);
      if (bucket == null) return;

      var dailyLog = bucket.DailyLogs.FirstOrDefault(l => l.Date.Date == explanation.WorkDate.Date);
      if (dailyLog == null) return;

      if (explanation.Type == ExplanationType.CompensatoryTime)
      {
          // Resolve shift to get standard hours for capping
          var shift = await GetEffectiveShiftAsync(explanation.EmployeeId, explanation.WorkDate);
          double standardHours = shift?.StandardWorkingHours ?? 8.0;

          // AddCompensatedHours caps at standardHours and returns actual consumed
          double actualUsed = dailyLog.AddCompensatedHours(explanation.RequestedCompHours, standardHours);

          // Confirm only the actually used hours
          bucket.ConfirmCompensatoryHours(actualUsed);

          // Return any excess that wasn't needed back to the pool
          double excess = explanation.RequestedCompHours - actualUsed;
          if (excess > 0)
          {
              bucket.CancelCompensatoryHours(excess);
          }

          bucket.AddOrUpdateDailyLog(dailyLog);
          bucket.RecalculateTotals();
          await _attendanceRepo.UpdateAsync(bucket.Id, bucket, cancellationToken);
      }
      else
      {
          // Resolve shift to get standard hours
          var shift = await GetEffectiveShiftAsync(explanation.EmployeeId, explanation.WorkDate);
          double approvedHours = shift?.StandardWorkingHours ?? 8.0;

          dailyLog.UpdateCalculationResults(
              workingHours: approvedHours,
              lateMinutes: dailyLog.LateMinutes,    // keep existing late flag
              earlyLeaveMinutes: 0,
              overtimeHours: 0,
              status: Domain.Enums.AttendanceStatus.Present,
              note: "[Đã giải trình] Quản lý đã phê duyệt",
              isLate: dailyLog.IsLate,
              isEarlyLeave: false,
              isMissingPunch: false,
              isMissingCheckIn: false);   // clear both missing flags

          bucket.AddOrUpdateDailyLog(dailyLog);
          bucket.RecalculateTotals();
          await _attendanceRepo.UpdateAsync(bucket.Id, bucket, cancellationToken);
      }
    }

    private async Task<Domain.Entities.Attendance.Shift?> GetEffectiveShiftAsync(string employeeId, DateTime date)
    {
      var rosterShift = await _shiftRepo.GetShiftByDateAsync(employeeId, date);
      if (rosterShift != null) return rosterShift;

      var employee = await _employeeRepo.GetByIdAsync(employeeId);
      if (!string.IsNullOrEmpty(employee?.JobDetails.ShiftId))
        return await _shiftRepo.GetByIdAsync(employee.JobDetails.ShiftId);

      return null;
    }
  }
}
