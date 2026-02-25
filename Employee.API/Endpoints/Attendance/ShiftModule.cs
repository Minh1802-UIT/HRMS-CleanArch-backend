using Carter;
using Employee.API.Common;
using Employee.Application.Features.Attendance.Dtos;

namespace Employee.API.Endpoints.Attendance
{
  public class ShiftModule : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      var group = app.MapGroup("/api/shifts")
                     .WithTags("Attendance - Shifts")
                     .RequireAuthorization(); // Mặc định yêu cầu login

      // GET: Ai cũng có thể xem (để chọn ca làm việc)
      group.MapGet("/", ShiftHandlers.GetPaged);
      group.MapGet("/lookup", ShiftHandlers.GetLookup); // New Lookup API
      group.MapGet("/{id}", ShiftHandlers.GetById);

      // CUD: Chỉ Admin/HR mới được cấu hình
      group.MapPost("/", ShiftHandlers.Create)
           .AddEndpointFilter<ValidationFilter<CreateShiftDto>>()
           .RequireAuthorization(p => p.RequireRole("Admin", "HR"));

      group.MapPut("/{id}", ShiftHandlers.Update)
           .AddEndpointFilter<ValidationFilter<UpdateShiftDto>>()
           .RequireAuthorization(p => p.RequireRole("Admin", "HR"));

      group.MapDelete("/{id}", ShiftHandlers.Delete)
           .RequireAuthorization(p => p.RequireRole("Admin", "HR"));
    }
  }
}