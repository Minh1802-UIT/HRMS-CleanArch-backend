using Employee.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Employee.Infrastructure.BackgroundServices
{
  /// <summary>
  /// Background service that periodically marks performance goals as overdue.
  /// Keeps read queries free of write side effects.
  /// </summary>
  public class PerformanceGoalOverdueBackgroundService : BackgroundService
  {
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PerformanceGoalOverdueBackgroundService> _logger;
    private readonly TimeSpan _interval;
    private const int MaxRetries = 3;

    public PerformanceGoalOverdueBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<PerformanceGoalOverdueBackgroundService> logger,
        IConfiguration configuration)
    {
      _scopeFactory = scopeFactory;
      _logger = logger;
      var hours = configuration.GetValue<int>(
          "BackgroundJobs:PerformanceGoalOverdueIntervalHours", 6);
      _interval = TimeSpan.FromHours(hours);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      _logger.LogInformation(
          "PerformanceGoalOverdueBackgroundService started. Interval: {Interval}h",
          _interval.TotalHours);

      await ExecuteWithRetryAsync(stoppingToken);

      while (!stoppingToken.IsCancellationRequested)
      {
        try
        {
          await Task.Delay(_interval, stoppingToken);
        }
        catch (OperationCanceledException)
        {
          break;
        }

        await ExecuteWithRetryAsync(stoppingToken);
      }

      _logger.LogInformation("PerformanceGoalOverdueBackgroundService stopped.");
    }

    private async Task ExecuteWithRetryAsync(CancellationToken stoppingToken)
    {
      for (int attempt = 1; attempt <= MaxRetries; attempt++)
      {
        try
        {
          using var scope = _scopeFactory.CreateScope();
          var repo = scope.ServiceProvider.GetRequiredService<IPerformanceGoalRepository>();
          var updated = await repo.MarkOverdueAsync(DateTime.UtcNow, stoppingToken);

          if (updated > 0)
          {
            _logger.LogInformation("Marked {Count} performance goal(s) as overdue.", updated);
          }
          else
          {
            _logger.LogDebug("No performance goals to mark overdue.");
          }

          return;
        }
        catch (Exception ex) when (attempt < MaxRetries && !stoppingToken.IsCancellationRequested)
        {
          var delay = TimeSpan.FromSeconds(attempt * 10);
          _logger.LogWarning(ex,
              "Overdue sweep attempt {Attempt}/{Max} failed. Retrying in {Delay}s...",
              attempt, MaxRetries, delay.TotalSeconds);
          await Task.Delay(delay, stoppingToken);
        }
        catch (Exception ex)
        {
          _logger.LogError(ex,
              "Overdue sweep failed after {Max} attempts.", MaxRetries);
          return;
        }
      }
    }
  }
}
