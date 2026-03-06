using Carter;
using Employee.API.Common;
using Employee.Application.Features.Organization.Dtos;

namespace Employee.API.Endpoints.Organization
{
  public class PositionModule : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      var group = app.MapGroup("/api/positions")
                     .WithTags("Organization - Positions")
                     .RequireAuthorization(); // Mặc định phải đăng nhập

      // 1. GET: Ai cũng xem được (để hiển thị dropdown chọn chức vụ)
      group.MapGet("/", PositionHandlers.GetPaged);
      group.MapGet("/tree", PositionHandlers.GetTree);
      group.MapGet("/{id}", PositionHandlers.GetById);

      // 2. CREATE: Chỉ Admin/HR
      group.MapPost("/", PositionHandlers.Create)
           .AddEndpointFilter<ValidationFilter<CreatePositionDto>>()
           .RequireAuthorization(p => p.RequireRole("Admin", "HR"));

      // 3. UPDATE: Chỉ Admin/HR
      group.MapPatch("/{id}", PositionHandlers.Update)
           .AddEndpointFilter<ValidationFilter<UpdatePositionDto>>()
           .RequireAuthorization(p => p.RequireRole("Admin", "HR"));

      // 4. DELETE: Chỉ Admin (Xóa chức vụ ảnh hưởng lớn đến hệ thống)
      group.MapDelete("/{id}", PositionHandlers.Delete)
           .RequireAuthorization(p => p.RequireRole("Admin"));
    }
  }
}