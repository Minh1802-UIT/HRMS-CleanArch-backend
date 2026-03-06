using Employee.Application.Features.Attendance.Dtos;
using Employee.Application.Features.Attendance.Mappers;
using Employee.Application.Common.Interfaces;
using Employee.Application.Common.Exceptions;
using Employee.Domain.Entities.Attendance;
using Employee.Domain.Interfaces.Repositories;
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

    public SubmitExplanationHandler(
        IAttendanceExplanationRepository repo,
        IAttendanceRepository attendanceRepo)
    {
      _repo = repo;
      _attendanceRepo = attendanceRepo;
    }

    public async Task<AttendanceExplanationDto> Handle(SubmitExplanationCommand request, CancellationToken cancellationToken)
    {
      if (string.IsNullOrWhiteSpace(request.Dto.Reason))
        throw new ValidationException("Reason is required.");

      // Validate that the work-date actually has IsMissingPunch or IsMissingCheckIn = true
      var monthKey = request.Dto.WorkDate.ToString("MM-yyyy");
      var bucket = await _attendanceRepo.GetByEmployeeAndMonthAsync(request.EmployeeId, monthKey);
      var dailyLog = bucket?.DailyLogs.FirstOrDefault(l => l.Date.Date == request.Dto.WorkDate.Date);

      if (dailyLog == null)
        throw new NotFoundException($"Không tìm thấy ngày công {request.Dto.WorkDate:dd/MM/yyyy}.");

      // Accept explanation for:
      //  - IsMissingPunch (checked in but no checkout, ghost-log may have run)
      //  - IsMissingCheckIn (checked out but no check-in)
      //  - Raw missing checkout: has CheckIn but no CheckOut and ghost-log hasn't processed yet
      bool isMissingCheckout = dailyLog.CheckIn.HasValue && !dailyLog.CheckOut.HasValue;
      if (!dailyLog.IsMissingPunch && !dailyLog.IsMissingCheckIn && !isMissingCheckout)
        throw new ConflictException("Ngày này không có trường hợp cần giải trình.");

      // Prevent duplicate pending submission
      var existing = await _repo.GetByEmployeeAndDateAsync(request.EmployeeId, request.Dto.WorkDate, cancellationToken);
      if (existing != null && existing.Status == Domain.Enums.ExplanationStatus.Pending)
        throw new ConflictException("Bạn đã có đơn giải trình đang chờ duyệt cho ngày này.");

      var explanation = new AttendanceExplanation(request.EmployeeId, request.Dto.WorkDate, request.Dto.Reason);
      await _repo.CreateAsync(explanation);

      return explanation.ToDto(employeeName: null);
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
