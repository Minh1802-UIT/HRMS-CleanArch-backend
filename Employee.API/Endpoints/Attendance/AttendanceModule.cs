using Carter;
using Employee.API.Common;
using Employee.Application.Features.Attendance.Dtos;

namespace Employee.API.Endpoints.Attendance
{
     public class AttendanceModule : ICarterModule
     {
          public void AddRoutes(IEndpointRouteBuilder app)
          {
               var group = app.MapGroup("/api/attendance")
                              .WithTags("Attendance - Operations")
                              .RequireAuthorization();

               // 1. Chấm công (User dùng hàng ngày)
               group.MapPost("/check-in", AttendanceHandlers.CheckIn)
                    .AddEndpointFilter<ValidationFilter<CheckInRequestDto>>()
                    .RequireRateLimiting("checkin");   // 10 events/hour per user

               group.MapPost("/check-out", AttendanceHandlers.CheckOut)
                    .AddEndpointFilter<ValidationFilter<CheckInRequestDto>>()
                    .RequireRateLimiting("checkin");   // shared bucket with check-in

               // 2. Xem công của mình
               group.MapGet("/me/range", AttendanceHandlers.GetMyRange);
               group.MapGet("/me/report", AttendanceHandlers.GetMyReport);

               // 3. Xem tổng hợp Team (Manager)
               group.MapGet("/team/summary", AttendanceHandlers.GetTeamSummary)
                    .RequireAuthorization(p => p.RequireRole("Admin", "HR", "Manager"));

               // 4. Xem công nhân viên (Chỉ Admin/HR/Manager)
               group.MapGet("/employee/{employeeId}/report", AttendanceHandlers.GetEmployeeReport)
                    .RequireAuthorization(p => p.RequireRole("Admin", "HR", "Manager"));

               // 4. Báo cáo ngày (Dashboard) - M3-UPDATE: POST to allow body with pagination
               group.MapPost("/daily/{dateStr}", AttendanceHandlers.GetDailyReport)
                    .RequireAuthorization(p => p.RequireRole("Admin", "HR", "Manager"));

               // 5. Kích hoạt xử lý (Admin/HR dùng để tổng hợp dữ liệu nháp vào báo cáo)
               group.MapPost("/process-logs", AttendanceHandlers.ProcessLogs)
                    .RequireAuthorization(p => p.RequireRole("Admin", "HR"));
          }
     }
}