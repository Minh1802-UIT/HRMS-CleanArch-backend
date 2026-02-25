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

               // 1. Lấy số dư của tôi
               group.MapGet("/me", LeaveAllocationHandlers.GetMyBalance);

               // 1.5 Lấy báo cáo toàn bộ (Admin/HR)
               group.MapPost("/list", LeaveAllocationHandlers.GetAllBalances)
                    .RequireAuthorization(p => p.RequireRole("Admin", "HR"));

               // 2. Lấy số dư của nhân viên cụ thể (HR/Admin) xem của người khác -> Update: Allow Owner too
               group.MapGet("/employee/{employeeId}", LeaveAllocationHandlers.GetBalanceByEmployee)
                    .RequireAuthorization();

               // 3. HR/Admin cấp phép
               group.MapPost("/", LeaveAllocationHandlers.Allocate)
                    .AddEndpointFilter<ValidationFilter<CreateAllocationDto>>()
                    .RequireAuthorization(p => p.RequireRole("Admin", "HR"));

               // 4. Thu hồi phép
               group.MapDelete("/{id}", LeaveAllocationHandlers.Delete)
                    .RequireAuthorization(p => p.RequireRole("Admin"));

               // 5. Initialize (Auto) — Admin/HR only
               group.MapPost("/initialize/{year}", LeaveAllocationHandlers.Initialize)
                    .RequireAuthorization(p => p.RequireRole("Admin", "HR"));

               // 6. Year-end carry-forward (NEW-5) — Admin only
               group.MapPost("/carry-forward/{fromYear}", LeaveAllocationHandlers.CarryForward)
                    .RequireAuthorization(p => p.RequireRole("Admin"));
          }
     }
}