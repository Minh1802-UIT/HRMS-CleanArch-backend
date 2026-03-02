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

               // === D�NH CHO NH�N VI�N (EMPLOYEE) ===
               // Xem l?ch s? luong c?a m�nh
               group.MapGet("/me", PayrollHandlers.GetMyHistory);

               // Xem chi ti?t 1 phi?u luong (ch? Admin/HR ho?c ch�nh ch? � handler s? ki?m tra ownership)
               group.MapGet("/{id}", PayrollHandlers.GetById)
                    .RequireAuthorization(p => p.RequireRole("Admin", "HR", "Employee"));

               // Xu?t PDF phi?u luong (ch? Admin/HR ho?c ch�nh ch?)
               group.MapGet("/{id}/pdf", PayrollHandlers.GetPdf)
                    .RequireAuthorization(p => p.RequireRole("Admin", "HR", "Employee"));

               // Xu?t Excel b?ng luong th�ng
               group.MapGet("/export", PayrollHandlers.ExportExcel)
                    .RequireAuthorization(p => p.RequireRole("Admin", "HR"));


               // === D�NH CHO QU?N L� (ADMIN / HR) ===

               // Xem b?ng luong to�n c�ng ty theo th�ng
               group.MapGet("/", PayrollHandlers.GetByMonth)
                          .RequireAuthorization(p => p.RequireRole("Admin", "HR"));

               // Ch?y t�nh luong (Generate)
               group.MapPost("/generate", PayrollHandlers.Generate)
                    .AddEndpointFilter<ValidationFilter<GeneratePayrollDto>>()
                    .RequireAuthorization(p => p.RequireRole("Admin", "HR"));

               // Duy?t luong / X�c nh?n d� tr?
               group.MapPut("/{id}/status", PayrollHandlers.UpdateStatus)
                    .AddEndpointFilter<ValidationFilter<UpdatePayrollStatusDto>>()
                    .RequireAuthorization(p => p.RequireRole("Admin", "HR"));

               // NEW-7: Annual PIT tax report
               group.MapGet("/tax-report/{year:int}", PayrollHandlers.GetTaxReport)
                    .RequireAuthorization(p => p.RequireRole("Admin", "HR"));

               // Xem l?ch s? luong c?a m?t nh�n vi�n c? th? (Admin/HR)
               group.MapGet("/employee/{employeeId}", PayrollHandlers.GetByEmployeeId)
                    .RequireAuthorization(p => p.RequireRole("Admin", "HR", "Manager"));
          }
     }
}
