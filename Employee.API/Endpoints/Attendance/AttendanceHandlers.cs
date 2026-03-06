using System.Linq;
using Employee.API.Common;
using Employee.Application.Features.Attendance.Dtos;
using Employee.Application.Common.Interfaces; // ICurrentUser
using Employee.Domain.Constants;
using Employee.Domain.Enums;
using Employee.Application.Common.Interfaces.Attendance.IService;
using Employee.Application.Common.Interfaces.Organization.IService;
using Employee.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Employee.Domain.Common.Models;
using Employee.Application.Features.Attendance.Commands.CheckIn;
using Microsoft.Extensions.Logging;

namespace Employee.API.Endpoints.Attendance
{
  public static class AttendanceHandlers
  {
    // Helper: resolve the EmployeeId for the current user.
    // JWT claim "EmployeeId" may be absent (e.g. newly-seeded users whose token
    // was not re-issued after account linking). Fall back to a DB lookup first;
    // only use UserId as last resort (which will cause processing to fail for
    // unlinked accounts — that's the correct behaviour).
    private static async Task<string> ResolveEmployeeIdAsync(ICurrentUser currentUser, IIdentityService identityService)
    {
      if (!string.IsNullOrEmpty(currentUser.EmployeeId))
        return currentUser.EmployeeId;

      // JWT has no EmployeeId claim — look it up from the Identity store
      var fromDb = await identityService.GetEmployeeIdByUserIdAsync(currentUser.UserId);
      return fromDb ?? string.Empty; // empty signals "unlinked account"
    }

    // 1. CHECK-IN / CHECK-OUT (CQRS)
    public static async Task<IResult> CheckIn(
        [FromBody] CheckInRequestDto dto,
        ISender sender,
        ICurrentUser currentUser,
        IIdentityService identityService)
    {
      dto.Type = "CheckIn"; // Force type

      string targetEmployeeId;
      if (!string.IsNullOrEmpty(dto.EmployeeId) && (currentUser.IsInRole("Admin") || currentUser.IsInRole("HR")))
      {
        targetEmployeeId = dto.EmployeeId;
      }
      else
      {
        targetEmployeeId = await ResolveEmployeeIdAsync(currentUser, identityService);
      }

      if (string.IsNullOrEmpty(targetEmployeeId))
      {
        return ResultUtils.Fail(ErrorCodes.UnlinkedAccount, "Tài khoản của bạn chưa được liên kết với hồ sơ nhân viên. Vui lòng liên hệ HR.");
      }

      var command = new CheckInCommand
      {
        Dto = dto,
        EmployeeId = targetEmployeeId
      };

      await sender.Send(command);
      return ResultUtils.Success($"Recorded {dto.Type} successfully.");
    }

    public static async Task<IResult> CheckOut(
        [FromBody] CheckInRequestDto dto,
        ISender sender,
        ICurrentUser currentUser,
        IIdentityService identityService)
    {
      dto.Type = "CheckOut"; // Force type

      string targetEmployeeId;
      if (!string.IsNullOrEmpty(dto.EmployeeId) && (currentUser.IsInRole("Admin") || currentUser.IsInRole("HR")))
      {
        targetEmployeeId = dto.EmployeeId;
      }
      else
      {
        targetEmployeeId = await ResolveEmployeeIdAsync(currentUser, identityService);
      }

      if (string.IsNullOrEmpty(targetEmployeeId))
      {
        return ResultUtils.Fail(ErrorCodes.UnlinkedAccount, "Tài khoản của bạn chưa được liên kết với hồ sơ nhân viên. Vui lòng liên hệ HR.");
      }

      var command = new CheckInCommand
      {
        Dto = dto,
        EmployeeId = targetEmployeeId
      };

      await sender.Send(command);
      return ResultUtils.Success($"Recorded {dto.Type} successfully.");
    }

    // 2. GET MY MONTHLY REPORT (User tự xem)
    public static async Task<IResult> GetMyReport(
        [FromQuery] string month,
        IAttendanceService service,
        ICurrentUser currentUser,
        IIdentityService identityService)
    {
      var employeeId = await ResolveEmployeeIdAsync(currentUser, identityService);
      if (string.IsNullOrEmpty(employeeId))
      {
        return ResultUtils.Fail(ErrorCodes.UnlinkedAccount, "Tài khoản của bạn chưa được liên kết với hồ sơ nhân viên. Vui lòng liên hệ HR.");
      }

      if (string.IsNullOrEmpty(month)) month = DateTime.UtcNow.ToString("MM-yyyy");

      var report = await service.GetMonthlyAttendanceAsync(employeeId, month);
      return ResultUtils.Success(report);
    }

    // 3. GET EMPLOYEE REPORT (Manager/HR views an employee's attendance)
    public static async Task<IResult> GetEmployeeReport(
        string employeeId,
        [FromQuery] string month,
        IAttendanceService service)
    {
      if (string.IsNullOrEmpty(month)) month = DateTime.UtcNow.ToString("MM-yyyy");

      var report = await service.GetMonthlyAttendanceAsync(employeeId, month);
      return ResultUtils.Success(report);
    }

    // 4. GET DAILY REPORT FOR ALL EMPLOYEES (Dashboard)
    public static async Task<IResult> GetDailyReport(
        string dateStr,
        [AsParameters] PaginationParams pagination,
        IAttendanceService service,
        IAttendanceRepository repo,
        IEmployeeRepository employeeRepo)
    {
      if (!DateTime.TryParse(dateStr, out var date))
      {
        date = DateTime.UtcNow;
      }

      // 1. Get paged employees
      var pagedEmployees = await employeeRepo.GetPagedAsync(pagination);
      
      // 2. Get attendance data for the month
      var monthStr = date.ToString("MM-yyyy");
      var buckets = (await repo.GetByMonthAsync(monthStr))
          .GroupBy(x => x.EmployeeId)
          .ToDictionary(g => g.Key, g => g.First());

      // 3. Map employees with attendance data (if exists)
      var dailyRecords = pagedEmployees.Items.Select(emp =>
      {
        buckets.TryGetValue(emp.Id, out var bucket);
        var log = bucket?.DailyLogs.FirstOrDefault(l => l.Date.Date == date.Date);
        
        return new
        {
          EmployeeId = emp.Id,
          EmployeeCode = emp.EmployeeCode,
          EmployeeName = emp.FullName,
          AvatarUrl = emp.AvatarUrl,
          Date = date,
          CheckIn = log?.CheckIn,
          CheckOut = log?.CheckOut,
          WorkingHours = log?.WorkingHours ?? 0,
          Status = log?.Status.ToString() ?? "Absent"
        };
      }).ToList();

      var result = new PagedResult<object>
      {
        Items = dailyRecords.Cast<object>().ToList(),
        TotalCount = pagedEmployees.TotalCount,
        PageNumber = pagination.PageNumber ?? 1,
        PageSize = pagination.PageSize ?? 10
      };

      return ResultUtils.Success(result);
    }

    public static async Task<IResult> GetMyRange(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        IAttendanceService service,
        ICurrentUser currentUser,
        IIdentityService identityService)
    {
      var employeeId = await ResolveEmployeeIdAsync(currentUser, identityService);
      if (string.IsNullOrEmpty(employeeId))
      {
        return ResultUtils.Fail(ErrorCodes.UnlinkedAccount, "Account not linked to employee profile.");
      }

      var result = await service.GetMyAttendanceRangeAsync(employeeId, from, to);
      return ResultUtils.Success(result);
    }

    public static async Task<IResult> GetTeamSummary(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        IAttendanceService service,
        ICurrentUser currentUser,
        IIdentityService identityService)
    {
      var employeeId = await ResolveEmployeeIdAsync(currentUser, identityService);
      if (string.IsNullOrEmpty(employeeId))
      {
        return ResultUtils.Fail(ErrorCodes.UnlinkedAccount, "Account not linked to employee profile.");
      }

      // Explicit role check
      if (!currentUser.IsInRole("Manager") && !currentUser.IsInRole("Admin") && !currentUser.IsInRole("HR"))
      {
        return ResultUtils.Fail("AUTH_FORBIDDEN", "You do not have permission to view team summary.");
      }

      var result = await service.GetTeamAttendanceSummaryAsync(employeeId, from, to);
      return ResultUtils.Success(result);
    }
    // 6. GET TODAY STATUS — used by the check-in page to lock the correct button
    public static async Task<IResult> GetTodayStatus(
        IRawAttendanceLogRepository rawRepo,
        ICurrentUser currentUser,
        IIdentityService identityService)
    {
      var employeeId = await ResolveEmployeeIdAsync(currentUser, identityService);
      if (string.IsNullOrEmpty(employeeId))
      {
        // Return a safe default: unlinked accounts can still see the page
        return ResultUtils.Success(new TodayAttendanceStatusDto());
      }

      // Compute today's window in UTC (system timezone: UTC+7)
      var utcOffset = TimeSpan.FromHours(7);
      var nowLocal = DateTime.UtcNow + utcOffset;
      var todayLocal = nowLocal.Date;
      var startUtc = todayLocal - utcOffset;           // 17:00 yesterday UTC
      var endUtc = todayLocal.AddDays(1) - utcOffset; // 17:00 today UTC

      var logs = await rawRepo.GetByDateRangeAsync(employeeId, startUtc, endUtc);

      var checkIns = logs.Where(l => l.Type == RawLogType.CheckIn).ToList();
      var checkOuts = logs.Where(l => l.Type == RawLogType.CheckOut).ToList();

      var status = new TodayAttendanceStatusDto
      {
        HasCheckedIn = checkIns.Any(),
        HasCheckedOut = checkOuts.Any(),
        // Return raw UTC — Angular date pipe handles the UTC→local (UTC+7) conversion
        CheckInTime = checkIns.Any() ? checkIns.Min(l => l.Timestamp) : null,
        CheckOutTime = checkOuts.Any() ? checkOuts.Max(l => l.Timestamp) : null,
      };

      return ResultUtils.Success(status);
    }
    // 7. TRIGGER PROCESSING (Manually trigger raw attendance aggregation)
    public static async Task<IResult> ProcessLogs(IAttendanceProcessingService processingService)
    {
      string result = await processingService.ProcessRawLogsAsync();
      return ResultUtils.Success(result);
    }
    // 8. FORCE REPROCESS MONTH
    //    Admin fix: resets already-processed raw logs for the given UTC month
    //    window back to unprocessed, then triggers ProcessRawLogsAsync.
    //    Used to repair corrupted AttendanceBuckets (e.g. after a BSON-mapping bug
    //    caused DailyLogs to not be persisted).
    public static async Task<IResult> ForceReprocessMonth(
        IRawAttendanceLogRepository rawRepo,
        IAttendanceProcessingService processingService,
        [FromQuery] string? month = null)
    {
      // Default: current month in UTC+7
      var utcOffset = TimeSpan.FromHours(7);
      var target = string.IsNullOrEmpty(month)
          ? DateTime.UtcNow + utcOffset
          : DateTime.ParseExact(month, "MM-yyyy", null);

      // First day of the target month (local time midnight) -> UTC, Kind=Utc required for MongoDB filter
      var firstLocal = new DateTime(target.Year, target.Month, 1);
      var lastLocal = firstLocal.AddMonths(1);
      var startUtc = DateTime.SpecifyKind(firstLocal - utcOffset, DateTimeKind.Utc);
      var endUtc = DateTime.SpecifyKind(lastLocal - utcOffset, DateTimeKind.Utc);

      // 1. Reset all processed logs in this window back to unprocessed
      var resetCount = await rawRepo.ResetProcessingStatusAsync(startUtc, endUtc);

      // 2. Reprocess
      var processResult = await processingService.ProcessRawLogsAsync();

      return ResultUtils.Success(new
      {
        ResetCount = resetCount,
        ProcessResult = processResult,
        Window = new { StartUtc = startUtc, EndUtc = endUtc }
      });
    }

    // 8. Backfill holiday flags for a specific month
    //    POST /api/attendance/admin/backfill-holidays
    //    Body: { "month": 2, "year": 2026 }
    public static async Task<IResult> BackfillHolidays(
        [FromBody] BackfillHolidaysRequest dto,
        IAttendanceProcessingService processingService)
    {
      if (dto.Month < 1 || dto.Month > 12)
        return ResultUtils.Fail("INVALID_MONTH", "Month must be between 1 and 12.");
      if (dto.Year < 2000 || dto.Year > 2100)
        return ResultUtils.Fail("INVALID_YEAR", "Year is out of range.");

      var updatedBuckets = await processingService.BackfillHolidayFlagsAsync(dto.Month, dto.Year);
      return ResultUtils.Success(new
      {
        UpdatedBuckets = updatedBuckets,
        Month = dto.Month,
        Year = dto.Year
      });
    }

    // ── EXPLANATION ──────────────────────────────────────────────────────────

    // 9. SUBMIT EXPLANATION (employee submits for a missing-punch day)
    public static async Task<IResult> SubmitExplanation(
        [FromBody] SubmitExplanationDto dto,
        ISender sender,
        ICurrentUser currentUser,
        IIdentityService identityService)
    {
      var employeeId = await ResolveEmployeeIdAsync(currentUser, identityService);
      if (string.IsNullOrEmpty(employeeId))
        return ResultUtils.Fail(ErrorCodes.UnlinkedAccount, "Tài khoản chưa liên kết với hồ sơ nhân viên.");

      var result = await sender.Send(new Employee.Application.Features.Attendance.Commands.Explanation.SubmitExplanationCommand
      {
        EmployeeId = employeeId,
        Dto = dto
      });
      return ResultUtils.Success(result);
    }

    // 10. GET MY EXPLANATIONS (employee views their own explanation history)
    public static async Task<IResult> GetMyExplanations(
        IAttendanceExplanationRepository repo,
        ICurrentUser currentUser,
        IIdentityService identityService)
    {
      var employeeId = await ResolveEmployeeIdAsync(currentUser, identityService);
      if (string.IsNullOrEmpty(employeeId))
        return ResultUtils.Fail(ErrorCodes.UnlinkedAccount, "Tài khoản chưa liên kết với hồ sơ nhân viên.");

      var list = await repo.GetByEmployeeIdAsync(employeeId);
      var result = list.Select(e => new
      {
        e.Id,
        e.EmployeeId,
        WorkDate = e.WorkDate,
        Reason = e.Reason,
        Status = e.Status.ToString(),
        ReviewedBy = e.ReviewedBy,
        ReviewNote = e.ReviewNote,
        ReviewedAt = e.ReviewedAt,
        CreatedAt = e.CreatedAt
      });
      return ResultUtils.Success(result);
    }

    // 11. GET PENDING EXPLANATIONS (manager/HR approval queue)
    public static async Task<IResult> GetPendingExplanations(
        IAttendanceExplanationRepository repo,
        IEmployeeRepository employeeRepo)
    {
      var pending = await repo.GetPendingAsync();
      var employeeIds = pending.Select(e => e.EmployeeId).Distinct().ToList();
      var nameMap = await employeeRepo.GetNamesByIdsAsync(employeeIds);

      var result = pending.Select(e => new
      {
        e.Id,
        e.EmployeeId,
        EmployeeName = nameMap.TryGetValue(e.EmployeeId, out var info) ? info.Name : null,
        WorkDate = e.WorkDate,
        Reason = e.Reason,
        Status = e.Status.ToString(),
        CreatedAt = e.CreatedAt
      });
      return ResultUtils.Success(result);
    }

    // 12. REVIEW EXPLANATION (manager approves or rejects)
    public static async Task<IResult> ReviewExplanation(
        string id,
        [FromBody] ReviewExplanationDto dto,
        ISender sender,
        ICurrentUser currentUser)
    {
      var result = await sender.Send(new Employee.Application.Features.Attendance.Commands.Explanation.ReviewExplanationCommand
      {
        ExplanationId = id,
        ReviewerUserId = currentUser.UserId,
        Dto = dto
      });
      return ResultUtils.Success(result);
    }
  }

  public record BackfillHolidaysRequest(int Month, int Year);
}
