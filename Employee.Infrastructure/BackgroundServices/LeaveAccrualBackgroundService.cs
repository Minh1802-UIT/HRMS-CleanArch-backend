using Employee.Application.Common.Interfaces.Organization.IService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Employee.Infrastructure.BackgroundServices
{
  /// <summary>
  /// P2-FIX: Background Service with retry logic and configurable schedule.
  /// Auto-accrues monthly leave for all active employees.
  /// </summary>
  public class LeaveAccrualBackgroundService : BackgroundService
  {
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LeaveAccrualBackgroundService> _logger;
    private readonly TimeSpan _interval;
    private const int MaxRetries = 3;

    public LeaveAccrualBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<LeaveAccrualBackgroundService> logger,
        IConfiguration configuration)
    {
      _scopeFactory = scopeFactory;
      _logger = logger;
      var hours = configuration.GetValue<int>("BackgroundJobs:LeaveAccrualIntervalHours", 6);
      _interval = TimeSpan.FromHours(hours);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      _logger.LogInformation("Leave Accrual Background Service started. Interval: {Interval}h", _interval.TotalHours);

      while (!stoppingToken.IsCancellationRequested)
      {
        await ExecuteWithRetryAsync(stoppingToken);

        try
        {
          await Task.Delay(_interval, stoppingToken);
        }
        catch (OperationCanceledException)
        {
          break;
        }
      }

      _logger.LogInformation("Leave Accrual Background Service stopped.");
    }

    private async Task ExecuteWithRetryAsync(CancellationToken stoppingToken)
    {
      for (int attempt = 1; attempt <= MaxRetries; attempt++)
      {
        try
        {
          using var scope = _scopeFactory.CreateScope();
          var leaveAllocationService = scope.ServiceProvider.GetRequiredService<ILeaveAllocationService>();

          await leaveAllocationService.RunMonthlyAccrualAsync();
          _logger.LogInformation("Monthly Leave Accrual completed successfully.");
          return;
        }
        catch (Exception ex) when (attempt < MaxRetries)
        {
          _logger.LogWarning(ex, "Leave accrual attempt {Attempt}/{Max} failed. Retrying in {Delay}s...",
              attempt, MaxRetries, attempt * 10);
          await Task.Delay(TimeSpan.FromSeconds(attempt * 10), stoppingToken);
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, "Leave accrual failed after {Max} attempts.", MaxRetries);
        }
      }
    }
  }
}
