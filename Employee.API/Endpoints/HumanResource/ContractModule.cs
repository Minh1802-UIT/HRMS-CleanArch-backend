using Carter;
using Employee.API.Common;
using Employee.Application.Features.HumanResource.Dtos;

namespace Employee.API.Endpoints.HumanResource
{
  public class ContractModule : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      var group = app.MapGroup("/api/contracts")
                     .WithTags("HumanResource - Contracts") // Tách tag cho dễ nhìn trên Swagger
                     .RequireAuthorization(); // Mặc định yêu cầu đăng nhập

      // 1. GET ALL (Admin/HR Only)
      group.MapGet("/", ContractHandlers.GetPaged)
           .RequireAuthorization(p => p.RequireRole("Admin", "HR"));

      // 2. GET BY ID (Admin/HR Only - Or owner logic in handler if needed)
      group.MapGet("/{id}", ContractHandlers.GetById)
           .RequireAuthorization(p => p.RequireRole("Admin", "HR"));

      // 2b. GET BY EMPLOYEE ID (Admin/HR Only)
      group.MapGet("/employee/{employeeId}", ContractHandlers.GetByEmployee);

      // 2c. GET MY CONTRACTS (Self-service)
      group.MapGet("/me", ContractHandlers.GetMyContracts);

      // 3. CREATE (Admin/HR Only)
      group.MapPost("/", ContractHandlers.Create)
           .AddEndpointFilter<ValidationFilter<CreateContractDto>>()
           .RequireAuthorization(p => p.RequireRole("Admin", "HR"));

      // 4. UPDATE (Admin/HR Only)
      group.MapPut("/{id}", ContractHandlers.Update)
           .AddEndpointFilter<ValidationFilter<UpdateContractDto>>()
           .RequireAuthorization(p => p.RequireRole("Admin", "HR"));

      // 5. DELETE (Admin Only)
      group.MapDelete("/{id}", ContractHandlers.Delete)
           .RequireAuthorization(p => p.RequireRole("Admin"));
    }
  }
}