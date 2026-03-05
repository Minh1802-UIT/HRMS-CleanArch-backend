using Employee.Application.Common.Interfaces;
using Hangfire;

namespace Employee.Infrastructure.Services
{
  /// <summary>
  /// Hangfire-backed implementation of IBackgroundJobService.
  /// Enqueues jobs into MongoDB-backed Hangfire storage — jobs survive restarts.
  /// </summary>
  public class HangfireBackgroundJobService : IBackgroundJobService
  {
    private readonly IBackgroundJobClient _client;

    public HangfireBackgroundJobService(IBackgroundJobClient client)
    {
      _client = client;
    }

    public void EnqueueAccountProvisioning(string employeeId, string email, string fullName, string phone)
    {
      _client.Enqueue<AccountProvisioningJob>(
          job => job.ExecuteAsync(employeeId, email, fullName, phone));
    }
  }
}
