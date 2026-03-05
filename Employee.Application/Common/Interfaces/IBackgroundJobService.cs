namespace Employee.Application.Common.Interfaces
{
  /// <summary>
  /// Abstraction over background job scheduling, keeping Application layer
  /// free of any direct Hangfire dependency.
  /// </summary>
  public interface IBackgroundJobService
  {
    /// <summary>
    /// Enqueues an account-provisioning job that creates an Identity account
    /// and sends a welcome email to the new employee.
    /// The job is persisted and will be retried automatically on failure.
    /// </summary>
    void EnqueueAccountProvisioning(string employeeId, string email, string fullName, string phone);
  }
}
