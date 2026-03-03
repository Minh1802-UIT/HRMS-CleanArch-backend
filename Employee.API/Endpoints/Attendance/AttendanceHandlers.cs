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

    // 3. GET EMPLOYEE REPORT (Manager/HR xem c?a nh�n vi�n)
    public static async Task<IResult> GetEmployeeReport(
        string employeeId,
        [FromQuery] string month,
        IAttendanceService service)
    {
      if (string.IsNullOrEmpty(month)) month = DateTime.UtcNow.ToString("MM-yyyy");

      var report = await service.GetMonthlyAttendanceAsync(employeeId, month);
      return ResultUtils.Success(report);
    }

    // 4. GET DAILY REPORT FOR ALL EMPLOYEES (Dashboard) - M3-UPDATE: Change to POST to support Paging
    public static async Task<IResult> GetDailyReport(
        string dateStr,
        [FromBody] PaginationParams pagination,
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

      // M3-FIX: Explicit role check for security & cleanup
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
        // Return local time so the UI can display it without offset math
        CheckInTime = checkIns.Any() ? (checkIns.Min(l => l.Timestamp) + utcOffset) : null,
        CheckOutTime = checkOuts.Any() ? (checkOuts.Max(l => l.Timestamp) + utcOffset) : null,
      };

      return ResultUtils.Success(status);
    }
    // 7. TRIGGER PROCESSING (K�ch ho?t t?ng h?p d? li?u)
    public static async Task<IResult> ProcessLogs(IAttendanceProcessingService processingService)
    {
      string result = await processingService.ProcessRawLogsAsync();
      return ResultUtils.Success(result);
    }
  }
}
