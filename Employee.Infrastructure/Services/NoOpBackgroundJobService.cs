using Employee.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Employee.Infrastructure.Services
{
  /// <summary>
  /// No-op implementation of IBackgroundJobService.
  /// Used when Redis/Hangfire is unavailable (e.g., in development or when Redis is down).
  /// Jobs are silently skipped instead of crashing the application.
  /// </summary>
  public class NoOpBackgroundJobService : IBackgroundJobService
  {
    private readonly ILogger<NoOpBackgroundJobService> _logger;

    public NoOpBackgroundJobService(ILogger<NoOpBackgroundJobService> logger)
    {
      _logger = logger;
    }

    public void EnqueueAccountProvisioning(string employeeId, string email, string fullName, string phone)
    {
      _logger.LogWarning(
        "Account provisioning job skipped (employeeId={EmployeeId}, email={Email}). " +
        "Background jobs are disabled because Redis/Hangfire is unavailable.",
        employeeId, email);
    }
  }
}
