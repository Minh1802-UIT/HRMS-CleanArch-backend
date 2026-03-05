using Hangfire.Dashboard;
using System.Security.Claims;

namespace Employee.API.Services
{
  /// <summary>
  /// Restricts the Hangfire dashboard to authenticated users with the Admin role.
  /// </summary>
  public class HangfireAuthFilter : IDashboardAuthorizationFilter
  {
    public bool Authorize(DashboardContext context)
    {
      var httpContext = context.GetHttpContext();

      if (!httpContext.User.Identity?.IsAuthenticated ?? true)
        return false;

      return httpContext.User.IsInRole("Admin");
    }
  }
}
