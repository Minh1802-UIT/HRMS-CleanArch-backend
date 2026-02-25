using Carter;
using Employee.API.Common;
using Employee.Application.Features.HumanResource.Dtos;

namespace Employee.API.Endpoints.HumanResource
{
  public class EmployeeModule : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      var group = app.MapGroup("/api/employees")
                     .WithTags("HumanResource - Employees")
                     .RequireAuthorization();

      group.MapPost("/list", EmployeeHandlers.GetPagedList)
           .WithName("GetPagedEmployeeList");

      group.MapGet("/lookup", EmployeeHandlers.GetLookup)
           .WithName("GetEmployeeLookup");

      group.MapGet("/org-chart", EmployeeHandlers.GetOrgChart)
           .WithName("GetEmployeeOrgChart");


      // 2. GET BY ID
      group.MapGet("/{id}", EmployeeHandlers.GetById);

      // 3. CREATE (HR/Admin Only)
      group.MapPost("/", EmployeeHandlers.Create)
           .AddEndpointFilter<ValidationFilter<CreateEmployeeDto>>()
           .RequireAuthorization(p => p.RequireRole("Admin", "HR"));

      // 4. UPDATE (HR/Admin Only)
      group.MapPut("/{id}", EmployeeHandlers.Update)
           .AddEndpointFilter<ValidationFilter<UpdateEmployeeDto>>()
           .RequireAuthorization(p => p.RequireRole("Admin", "HR"));

      // 5. DELETE (Admin Only)
      group.MapDelete("/{id}", EmployeeHandlers.Delete)
           .RequireAuthorization(p => p.RequireRole("Admin"));
    }
  }
}