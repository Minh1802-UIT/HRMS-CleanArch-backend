using Carter;
using Employee.Application.Common.Interfaces.Organization.IService;

namespace Employee.API.Endpoints.Common
{
  public class DashboardModule : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapGroup("/api/dashboard")
         .WithTags("Common - Dashboard")
         .RequireAuthorization(p => p.RequireRole("Admin", "HR"))
         .MapGet("/", DashboardHandlers.GetDashboardData);
    }
  }
}
