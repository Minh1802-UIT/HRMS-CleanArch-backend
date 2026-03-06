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

               // === EMPLOYEE ROUTES ===
               // View own payroll history
               group.MapGet("/me", PayrollHandlers.GetMyHistory);

               // View a single payslip (Admin/HR or the payslip owner — ownership enforced in handler)
               group.MapGet("/{id}", PayrollHandlers.GetById)
                    .RequireAuthorization(p => p.RequireRole("Admin", "HR", "Employee"));

               // Download payslip PDF (Admin/HR or own payslip)
               group.MapGet("/{id}/pdf", PayrollHandlers.GetPdf)
                    .RequireAuthorization(p => p.RequireRole("Admin", "HR", "Employee"));

               // Export monthly payroll as Excel
               group.MapGet("/export", PayrollHandlers.ExportExcel)
                    .RequireAuthorization(p => p.RequireRole("Admin", "HR"));


               // === ADMIN / HR ROUTES ===

               // Company-wide payroll list for a given month
               group.MapGet("/", PayrollHandlers.GetByMonth)
                          .RequireAuthorization(p => p.RequireRole("Admin", "HR"));

               // Run payroll calculation
               group.MapPost("/generate", PayrollHandlers.Generate)
                    .AddEndpointFilter<ValidationFilter<GeneratePayrollDto>>()
                    .RequireAuthorization(p => p.RequireRole("Admin", "HR"));

               // Approve / confirm payroll payment
               group.MapPost("/{id}/status", PayrollHandlers.UpdateStatus)
                    .AddEndpointFilter<ValidationFilter<UpdatePayrollStatusDto>>()
                    .RequireAuthorization(p => p.RequireRole("Admin", "HR"));

               // Annual PIT tax report
               group.MapGet("/tax-report/{year:int}", PayrollHandlers.GetTaxReport)
                    .RequireAuthorization(p => p.RequireRole("Admin", "HR"));

               // Payroll history for a specific employee (Admin/HR/Manager)
               group.MapGet("/employee/{employeeId}", PayrollHandlers.GetByEmployeeId)
                    .RequireAuthorization(p => p.RequireRole("Admin", "HR", "Manager"));
          }
     }
}