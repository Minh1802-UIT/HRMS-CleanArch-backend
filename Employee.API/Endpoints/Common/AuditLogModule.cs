using Carter;
using Employee.Application.Common.Interfaces.Organization.IService;

namespace Employee.API.Endpoints.Common
{
  public class AuditLogModule : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      app.MapGroup("/api/auditlogs")
         .WithTags("Common - Audit Logs")
         .RequireAuthorization(p => p.RequireRole("Admin"))
         .MapGet("/", AuditLogHandlers.GetLogs);
    }
  }
}
