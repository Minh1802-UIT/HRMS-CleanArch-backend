using Carter;
using Employee.API.Common;
using Employee.Application.Features.Leave.Dtos;

namespace Employee.API.Endpoints.Leave
{
  public class LeaveTypeModule : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      var group = app.MapGroup("/api/leave-types")
                     .WithTags("Leave Management - Types")
                     .RequireAuthorization();

      // GET thì ai cũng được dùng
      group.MapGet("/", LeaveTypeHandlers.GetPaged);
      group.MapGet("/{id}", LeaveTypeHandlers.GetById);

      // CUD thì chỉ ADMIN mới được dùng
      group.MapPost("/", LeaveTypeHandlers.Create)
           .AddEndpointFilter<ValidationFilter<CreateLeaveTypeDto>>()
           .RequireAuthorization(p => p.RequireRole("Admin"));

      group.MapPatch("/{id}", LeaveTypeHandlers.Update)
           .AddEndpointFilter<ValidationFilter<UpdateLeaveTypeDto>>()
           .RequireAuthorization(p => p.RequireRole("Admin"));

      group.MapDelete("/{id}", LeaveTypeHandlers.Delete)
           .RequireAuthorization(p => p.RequireRole("Admin"));
    }
  }
}