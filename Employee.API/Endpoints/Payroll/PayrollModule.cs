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
               // Xem l?ch s? luong c?a mình
               group.MapGet("/me", PayrollHandlers.GetMyHistory);

               // Xem chi ti?t 1 phi?u luong (ch? Admin/HR ho?c chính ch? — handler s? ki?m tra ownership)
               group.MapGet("/{id}", PayrollHandlers.GetById)
                    .RequireAuthorization(p => p.RequireRole("Admin", "HR", "Employee"));

               // Xu?t PDF phi?u luong (ch? Admin/HR ho?c chính ch?)
               group.MapGet("/{id}/pdf", PayrollHandlers.GetPdf)
                    .RequireAuthorization(p => p.RequireRole("Admin", "HR", "Employee"));

               // Xu?t Excel b?ng luong tháng
               group.MapGet("/export", PayrollHandlers.ExportExcel)
                    .RequireAuthorization(p => p.RequireRole("Admin", "HR"));


               // === DÀNH CHO QU?N LÝ (ADMIN / HR) ===

               // Xem b?ng luong toàn công ty theo tháng
               group.MapGet("/", PayrollHandlers.GetByMonth)
                          .RequireAuthorization(p => p.RequireRole("Admin", "HR"));

               // Ch?y tính luong (Generate)
               group.MapPost("/generate", PayrollHandlers.Generate)
                    .AddEndpointFilter<ValidationFilter<GeneratePayrollDto>>()
                    .RequireAuthorization(p => p.RequireRole("Admin", "HR"));

               // Duy?t luong / Xác nh?n dã tr?
               group.MapPut("/{id}/status", PayrollHandlers.UpdateStatus)
                    .AddEndpointFilter<ValidationFilter<UpdatePayrollStatusDto>>()
                    .RequireAuthorization(p => p.RequireRole("Admin", "HR"));

               // NEW-7: Annual PIT tax report
               group.MapGet("/tax-report/{year:int}", PayrollHandlers.GetTaxReport)
                    .RequireAuthorization(p => p.RequireRole("Admin", "HR"));
          }
     }
}
