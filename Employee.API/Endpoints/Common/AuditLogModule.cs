using Carter;
using Employee.Application.Common.Interfaces.Organization.IService;

namespace Employee.API.Endpoints.Common
{
  public class AuditLogModule : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      var group = app.MapGroup("/api/auditlogs")
         .WithTags("Common - Audit Logs")
         .RequireAuthorization(p => p.RequireRole("Admin"));

      // Offset-based (backward compat)
      group.MapGet("/", AuditLogHandlers.GetLogs);

      // Cursor-based (preferred — avoids Skip on 250 K+ docs)
      group.MapGet("/cursor", AuditLogHandlers.GetLogsCursor);
    }
  }
}
