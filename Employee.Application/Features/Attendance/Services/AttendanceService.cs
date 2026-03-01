using Employee.Application.Common.Interfaces.Attendance.IService;
using Employee.Domain.Interfaces.Repositories;
using Employee.Application.Features.Attendance.Dtos;
using Employee.Application.Features.Attendance.Mappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Employee.Application.Features.Attendance.Services
{
  public class AttendanceService : IAttendanceService
  {
    private readonly IAttendanceRepository _attendanceRepo;
    private readonly IEmployeeRepository _employeeRepo;

    public AttendanceService(IAttendanceRepository attendanceRepo, IEmployeeRepository employeeRepo)
    {
      _attendanceRepo = attendanceRepo;
      _employeeRepo = employeeRepo;
    }

    public async Task<MonthlyAttendanceDto> GetMonthlyAttendanceAsync(string employeeId, string month)
    {
      var bucket = await _attendanceRepo.GetByEmployeeAndMonthAsync(employeeId, month);

      if (bucket == null)
      {
        return new MonthlyAttendanceDto
        {
          EmployeeId = employeeId,
          Month = month,
          Logs = new List<DailyLogDto>()
        };
      }

      return bucket.ToDto();
    }

    public async Task<AttendanceRangeDto> GetMyAttendanceRangeAsync(string employeeId, DateTime fromDate, DateTime toDate)
    {
      var months = new HashSet<string>();
      var current = fromDate;
      while (current <= toDate)
      {
        months.Add(current.ToString("MM-yyyy"));
        current = current.AddMonths(1);
      }

      var buckets = await _attendanceRepo.GetByMonthsAsync(months);

      var relevantBuckets = buckets.Where(b => b.EmployeeId == employeeId);
      var allLogs = new List<DailyLogDto>();

      foreach (var bucket in relevantBuckets)
      {
        var dto = bucket.ToDto();
        allLogs.AddRange(dto.Logs);
      }

      var rangeLogs = allLogs
          .Where(l => l.Date.Date >= fromDate.Date && l.Date.Date <= toDate.Date)
          .OrderBy(l => l.Date)
          .ToList();

      return new AttendanceRangeDto
      {
        EmployeeId = employeeId,
        FromDate = fromDate,
        ToDate = toDate,
        Logs = rangeLogs,
        TotalWorkingHours = rangeLogs.Sum(l => l.WorkingHours),
        TotalOvertimeHours = rangeLogs.Sum(l => l.OvertimeHours)
      };
    }

    public async Task<TeamAttendanceSummaryDto> GetTeamAttendanceSummaryAsync(string managerId, DateTime fromDate, DateTime toDate)
    {
      var employees = await _employeeRepo.GetByManagerIdAsync(managerId);

      var months = new HashSet<string>();
      var current = fromDate;
      while (current <= toDate)
      {
        months.Add(current.ToString("MM-yyyy"));
        current = current.AddMonths(1);
      }

      var buckets = await _attendanceRepo.GetByMonthsAsync(months);
      var bucketMap = buckets.GroupBy(b => b.EmployeeId).ToDictionary(g => g.Key, g => g.ToList());

      var result = new TeamAttendanceSummaryDto
      {
        FromDate = fromDate,
        ToDate = toDate,
        Members = new List<TeamMemberAttendanceDto>()
      };

      foreach (var emp in employees)
      {
        var memberDto = new TeamMemberAttendanceDto
        {
          EmployeeId = emp.Id,
          EmployeeName = emp.FullName,
          Avatar = emp.AvatarUrl ?? "https://ui-avatars.com/api/?name=" + Uri.EscapeDataString(emp.FullName),
          EmployeeType = "Full Time",
          Department = emp.JobDetails?.DepartmentId ?? "N/A",
          Office = emp.PersonalInfo?.City ?? "N/A",
        };

        if (bucketMap.TryGetValue(emp.Id, out var empBuckets))
        {
          var allLogs = empBuckets.SelectMany(b => b.ToDto().Logs)
              .Where(l => l.Date.Date >= fromDate.Date && l.Date.Date <= toDate.Date)
              .OrderBy(l => l.Date)
              .ToList();

          memberDto.TotalWorkedHours = allLogs.Sum(l => l.WorkingHours);
          memberDto.Overtime = allLogs.Sum(l => l.OvertimeHours);

          var dayCount = (toDate.Date - fromDate.Date).Days + 1;
          for (int i = 0; i < dayCount; i++)
          {
            var d = fromDate.Date.AddDays(i);
            var log = allLogs.FirstOrDefault(l => l.Date.Date == d);
            memberDto.DailyHours.Add(log?.WorkingHours ?? 0);
          }
        }
        else
        {
          var dayCount = (toDate.Date - fromDate.Date).Days + 1;
          memberDto.DailyHours.AddRange(Enumerable.Repeat(0.0, dayCount));
        }

        result.Members.Add(memberDto);
      }

      return result;
    }
  }
}
