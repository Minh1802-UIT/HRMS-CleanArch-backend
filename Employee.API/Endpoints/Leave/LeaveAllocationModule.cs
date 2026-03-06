using Carter;
using Employee.API.Common;
using Employee.Application.Features.Leave.Dtos;

namespace Employee.API.Endpoints.Leave
{
     public class LeaveAllocationModule : ICarterModule
     {
          public void AddRoutes(IEndpointRouteBuilder app)
          {
               var group = app.MapGroup("/api/leave-allocations")
                              .WithTags("Leave Management - Allocation")
                              .RequireAuthorization();

               // 1. My leave balance
               group.MapGet("/me", LeaveAllocationHandlers.GetMyBalance);

               // 1.5 Full allocation report (Admin/HR) — GET with query params
               group.MapGet("/", LeaveAllocationHandlers.GetAllBalances)
                    .RequireAuthorization(p => p.RequireRole("Admin", "HR"));

               // 1.6 Full allocation report via POST body (used by Angular)
               group.MapPost("/list", LeaveAllocationHandlers.GetAllBalancesList)
                    .RequireAuthorization(p => p.RequireRole("Admin", "HR"));

               // 2. Balance for a specific employee (HR/Admin, or the employee themselves)
               group.MapGet("/employee/{employeeId}", LeaveAllocationHandlers.GetBalanceByEmployee)
                    .RequireAuthorization();

               // 3. Allocate / adjust leave days (HR/Admin)
               group.MapPost("/", LeaveAllocationHandlers.Allocate)
                    .AddEndpointFilter<ValidationFilter<CreateAllocationDto>>()
                    .RequireAuthorization(p => p.RequireRole("Admin", "HR"));

               // 4. Revoke allocation
               group.MapDelete("/{id}", LeaveAllocationHandlers.Delete)
                    .RequireAuthorization(p => p.RequireRole("Admin"));

               // 5. Initialize allocations for a new year (Admin/HR)
               group.MapPost("/initialize/{year}", LeaveAllocationHandlers.Initialize)
                    .RequireAuthorization(p => p.RequireRole("Admin", "HR"));

               // 6. Year-end carry-forward (Admin only)
               group.MapPost("/carry-forward/{fromYear}", LeaveAllocationHandlers.CarryForward)
                    .RequireAuthorization(p => p.RequireRole("Admin"));
          }
     }
}