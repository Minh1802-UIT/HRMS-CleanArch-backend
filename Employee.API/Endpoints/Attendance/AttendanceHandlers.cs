using System.Linq;
using Employee.API.Common;
using Employee.Application.Features.Attendance.Dtos;
using Employee.Application.Common.Interfaces; // ICurrentUser
using Employee.Domain.Constants;
using Employee.Application.Common.Interfaces.Attendance.IService;
using Employee.Application.Common.Interfaces.Organization.IService;
using Employee.Application.Common.Interfaces.Organization.IRepository;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Employee.Application.Common.Models;
using Employee.Application.Features.Attendance.Commands.CheckIn;

namespace Employee.API.Endpoints.Attendance
{
  public static class AttendanceHandlers
  {
    // 1. CHECK-IN / CHECK-OUT (CQRS)
    public static async Task<IResult> CheckIn(
        [FromBody] CheckInRequestDto dto,
        ISender sender, // Use Sender
        ICurrentUser currentUser)
    {
      dto.Type = "CheckIn"; // Force type
      string targetEmployeeId = currentUser.EmployeeId ?? currentUser.UserId;
      if (!string.IsNullOrEmpty(dto.EmployeeId) && (currentUser.IsInRole("Admin") || currentUser.IsInRole("HR")))
      {
          targetEmployeeId = dto.EmployeeId;
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
        ICurrentUser currentUser)
    {
      dto.Type = "CheckOut"; // Force type
      string targetEmployeeId = currentUser.EmployeeId ?? currentUser.UserId;
      if (!string.IsNullOrEmpty(dto.EmployeeId) && (currentUser.IsInRole("Admin") || currentUser.IsInRole("HR")))
      {
          targetEmployeeId = dto.EmployeeId;
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
        ICurrentUser currentUser)
    {
      if (string.IsNullOrEmpty(currentUser.EmployeeId))
      {
        return ResultUtils.Fail(ErrorCodes.UnlinkedAccount, "Tài khoản của bạn chưa được liên kết với hồ sơ nhân viên. Vui lòng liên hệ HR.");
      }

      if (string.IsNullOrEmpty(month)) month = DateTime.UtcNow.ToString("MM-yyyy");

      var report = await service.GetMonthlyAttendanceAsync(currentUser.EmployeeId, month);
      return ResultUtils.Success(report);
    }

    // 3. GET EMPLOYEE REPORT (Manager/HR xem của nhân viên)
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
        ICurrentUser currentUser)
    {
      if (string.IsNullOrEmpty(currentUser.EmployeeId))
      {
        return ResultUtils.Fail("AUTH_UNLINKED", "Account not linked to employee profile.");
      }

      var result = await service.GetMyAttendanceRangeAsync(currentUser.EmployeeId, from, to);
      return ResultUtils.Success(result);
    }

    public static async Task<IResult> GetTeamSummary(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        IAttendanceService service,
        ICurrentUser currentUser)
    {
      if (string.IsNullOrEmpty(currentUser.EmployeeId))
      {
        return ResultUtils.Fail("AUTH_UNLINKED", "Account not linked to employee profile.");
      }

      // M3-FIX: Explicit role check for security & cleanup
      if (!currentUser.IsInRole("Manager") && !currentUser.IsInRole("Admin") && !currentUser.IsInRole("HR"))
      {
        return ResultUtils.Fail("AUTH_FORBIDDEN", "You do not have permission to view team summary.");
      }

      var result = await service.GetTeamAttendanceSummaryAsync(currentUser.EmployeeId, from, to);
      return ResultUtils.Success(result);
    }

    // 7. TRIGGER PROCESSING (Kích hoạt tổng hợp dữ liệu)
    public static async Task<IResult> ProcessLogs(IAttendanceProcessingService processingService)
    {
      string result = await processingService.ProcessRawLogsAsync();
      return ResultUtils.Success(result);
    }
  }
}