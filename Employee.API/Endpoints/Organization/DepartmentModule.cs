using Carter;

using Employee.API.Endpoints.Organization;
using Employee.Application.Features.Organization.Dtos;
using Employee.API.Common;

namespace Employee.API.Endpoints.Organization;

public class DepartmentModule : ICarterModule
{
  public void AddRoutes(IEndpointRouteBuilder app)
  {
    var group = app.MapGroup("/api/departments")
                   .WithTags("Organization-Department")
                   .RequireAuthorization();

    group.MapGet("/", DepartmentHandlers.GetPaged);
    group.MapGet("/tree", DepartmentHandlers.GetTree);
    group.MapGet("/{id}", DepartmentHandlers.GetById);

    group.MapPost("/", DepartmentHandlers.Create)
         .AddEndpointFilter<ValidationFilter<CreateDepartmentDto>>()
         .RequireAuthorization(p => p.RequireRole("Admin")); // Chỉ Admin tạo phòng ban

          group.MapPatch("/{id}", DepartmentHandlers.Update)
               .AddEndpointFilter<ValidationFilter<UpdateDepartmentDto>>()
         .RequireAuthorization(p => p.RequireRole("Admin"));

    group.MapDelete("/{id}", DepartmentHandlers.Delete)
         .RequireAuthorization(p => p.RequireRole("Admin"));
  }
}