using Carter;
using Microsoft.Extensions.Hosting;

namespace Employee.API.Endpoints.Dev
{
  /// <summary>
  /// Dev/test-only routes. Only registered when running in Development environment.
  /// </summary>
  public class DevModule : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      var env = app.ServiceProvider.GetRequiredService<IHostEnvironment>();
      if (!env.IsDevelopment()) return;

      var group = app.MapGroup("/api/dev")
                     .WithTags("Dev Tools")
                     .RequireAuthorization();

      // POST /api/dev/seed-attendance?month=02-2026
      // Seeds full Mon-Fri attendance data for all active employees for the given month.
      group.MapPost("/seed-attendance", DevHandlers.SeedAttendance)
           .RequireAuthorization(p => p.RequireRole("Admin"));

      // ── Simulation Engine (Dev shortcuts) ──────────────────────────────────
      group.MapPost("/simulation/provision", DevHandlers.ProvisionSimulation)
           .RequireAuthorization(p => p.RequireRole("Admin"));

      group.MapPost("/simulation/run", DevHandlers.RunSimulation)
           .RequireAuthorization(p => p.RequireRole("Admin"));

      group.MapGet("/simulation/dashboard", DevHandlers.GetSimulationDashboard)
           .RequireAuthorization(p => p.RequireRole("Admin"));
    }
  }
}
