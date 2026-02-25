using Employee.Application.Features.Attendance.Dtos;

namespace Employee.Application.Common.Interfaces.Attendance.IService
{
  public interface IAttendanceService
  {
    // Ghi log check-in/out
    // LogCheckInAsync removed (Moved to CQRS: CheckInCommand)

    // Xem bảng công tháng (Bucket)
    Task<MonthlyAttendanceDto> GetMonthlyAttendanceAsync(string employeeId, string month); // month format: "MM-yyyy"

    // Xem bảng công theo khoảng thời gian (cho Time Tracking UI)
    Task<AttendanceRangeDto> GetMyAttendanceRangeAsync(string employeeId, DateTime fromDate, DateTime toDate);

    // Xem tổng hợp Team (cho Manager)
    Task<TeamAttendanceSummaryDto> GetTeamAttendanceSummaryAsync(string managerId, DateTime fromDate, DateTime toDate);
  }
}