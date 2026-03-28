using Employee.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Employee.Infrastructure.Services;

/// <summary>
/// No-op IBackgroundJobService for Development mode (no Redis/Hangfire).
/// All background job calls are logged but not persisted.
/// </summary>
public class DevBackgroundJobService : IBackgroundJobService
{
    private readonly ILogger<DevBackgroundJobService> _logger;

    public DevBackgroundJobService(ILogger<DevBackgroundJobService> logger)
    {
        _logger = logger;
    }

    public void EnqueueAccountProvisioning(string employeeId, string email, string fullName, string phone)
    {
        _logger.LogDebug(
            "[DevBackgroundJobService] AccountProvisioning skipped in Dev: EmployeeId={EmployeeId}, Email={Email}",
            employeeId, email);
    }
}
