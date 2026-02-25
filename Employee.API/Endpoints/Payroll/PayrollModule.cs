using Carter;
using Employee.API.Common;
using Employee.Application.Features.Payroll.Dtos;

namespace Employee.API.Endpoints.Payroll
{
     public class PayrollModule : ICarterModule
     {
          public void AddRoutes(IEndpointRouteBuilder app)
          {
               var group = app.MapGroup("/api/payrolls")
                              .WithTags("Payroll Management")
                              .RequireAuthorization();

               // === DÀNH CHO NHÂN VIÊN (EMPLOYEE) ===
               // Xem lịch sử lương của mình
               group.MapGet("/me", PayrollHandlers.GetMyHistory);

               // Xem chi tiết 1 phiếu lương (chỉ Admin/HR hoặc chính chủ — handler sẽ kiểm tra ownership)
               group.MapGet("/{id}", PayrollHandlers.GetById)
                    .RequireAuthorization(p => p.RequireRole("Admin", "HR", "Employee"));

               // Xuất PDF phiếu lương (chỉ Admin/HR hoặc chính chủ)
               group.MapGet("/{id}/pdf", PayrollHandlers.GetPdf)
                    .RequireAuthorization(p => p.RequireRole("Admin", "HR", "Employee"));

               // Xuất Excel bảng lương tháng
               group.MapGet("/export", PayrollHandlers.ExportExcel)
                    .RequireAuthorization(p => p.RequireRole("Admin", "HR"));


               // === DÀNH CHO QUẢN LÝ (ADMIN / HR) ===

               // Xem bảng lương toàn công ty theo tháng
               group.MapGet("/", PayrollHandlers.GetByMonth)
                          .RequireAuthorization(p => p.RequireRole("Admin", "HR"));

               // Chạy tính lương (Generate)
               group.MapPost("/generate", PayrollHandlers.Generate)
                    .AddEndpointFilter<ValidationFilter<GeneratePayrollDto>>()
                    .RequireAuthorization(p => p.RequireRole("Admin", "HR"));

               // Duyệt lương / Xác nhận đã trả
               group.MapPut("/{id}/status", PayrollHandlers.UpdateStatus)
                    .AddEndpointFilter<ValidationFilter<UpdatePayrollStatusDto>>()
                    .RequireAuthorization(p => p.RequireRole("Admin", "HR"));

               // NEW-7: Annual PIT tax report
               group.MapGet("/tax-report/{year:int}", PayrollHandlers.GetTaxReport)
                    .RequireAuthorization(p => p.RequireRole("Admin", "HR"));
          }
     }
}