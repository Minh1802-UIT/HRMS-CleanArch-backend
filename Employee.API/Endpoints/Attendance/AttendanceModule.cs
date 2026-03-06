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
               group.MapGet("/me/today-status", AttendanceHandlers.GetTodayStatus);
               group.MapGet("/me/range", AttendanceHandlers.GetMyRange);
               group.MapGet("/me/report", AttendanceHandlers.GetMyReport);

               // 3. Xem tổng hợp Team (Manager)
               group.MapGet("/team/summary", AttendanceHandlers.GetTeamSummary)
                    .RequireAuthorization(p => p.RequireRole("Admin", "HR", "Manager"));

               // 4. Xem công nhân viên (Chỉ Admin/HR/Manager)
               group.MapGet("/employee/{employeeId}/report", AttendanceHandlers.GetEmployeeReport)
                    .RequireAuthorization(p => p.RequireRole("Admin", "HR", "Manager"));

               // 4. Báo cáo ngày (Dashboard)
               group.MapGet("/daily/{dateStr}", AttendanceHandlers.GetDailyReport)
                    .RequireAuthorization(p => p.RequireRole("Admin", "HR", "Manager"));

               // 5. Kích hoạt xử lý (Admin/HR dùng để tổng hợp dữ liệu nháp vào báo cáo)
               group.MapPost("/process-logs", AttendanceHandlers.ProcessLogs)
                    .RequireAuthorization(p => p.RequireRole("Admin", "HR"));

               // 6. Force-reprocess a month (Admin repair — fixes corrupted buckets)
               //    POST /api/attendance/admin/force-reprocess?month=03-2026
               group.MapPost("/admin/force-reprocess", AttendanceHandlers.ForceReprocessMonth)
                    .RequireAuthorization(p => p.RequireRole("Admin"));

               // 7. Backfill holiday flags for existing attendance records
               //    POST /api/attendance/admin/backfill-holidays  { month: 2, year: 2026 }
               group.MapPost("/admin/backfill-holidays", AttendanceHandlers.BackfillHolidays)
                    .RequireAuthorization(p => p.RequireRole("Admin", "HR"));

               // 8. EXPLANATION — nhân viên giải trình quên check-out
               group.MapPost("/explanation", AttendanceHandlers.SubmitExplanation);
               group.MapGet("/explanation/me", AttendanceHandlers.GetMyExplanations);
               group.MapGet("/explanation/pending", AttendanceHandlers.GetPendingExplanations)
                    .RequireAuthorization(p => p.RequireRole("Admin", "HR", "Manager"));
               group.MapPut("/explanation/{id}/review", AttendanceHandlers.ReviewExplanation)
                    .RequireAuthorization(p => p.RequireRole("Admin", "HR", "Manager"));

               // 9. OVERTIME SCHEDULE — Admin/HR setup approved OT dates
               group.MapPost("/overtime-schedule", AttendanceHandlers.CreateOvertimeSchedule)
                    .RequireAuthorization(p => p.RequireRole("Admin", "HR"));
               group.MapPost("/overtime-schedule/bulk", AttendanceHandlers.CreateBulkOvertimeSchedule)
                    .RequireAuthorization(p => p.RequireRole("Admin", "HR"));
               group.MapDelete("/overtime-schedule/{id}", AttendanceHandlers.DeleteOvertimeSchedule)
                    .RequireAuthorization(p => p.RequireRole("Admin", "HR"));
               group.MapGet("/overtime-schedule", AttendanceHandlers.GetOvertimeSchedulesByMonth)
                    .RequireAuthorization(p => p.RequireRole("Admin", "HR", "Manager"));
          }
     }
}