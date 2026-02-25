using Employee.Application.Common.Interfaces.Organization.IService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Employee.Infrastructure.BackgroundServices
{
  /// <summary>
  /// P2-FIX: Background Service with retry logic and configurable schedule.
  /// Auto-calculates monthly payroll near end of month.
  /// </summary>
  public class PayrollBackgroundService : BackgroundService
  {
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PayrollBackgroundService> _logger;
    private readonly TimeSpan _interval;
    private const int MaxRetries = 3;

    public PayrollBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<PayrollBackgroundService> logger,
        IConfiguration configuration)
    {
      _scopeFactory = scopeFactory;
      _logger = logger;
      var hours = configuration.GetValue<int>("BackgroundJobs:PayrollIntervalHours", 12);
      _interval = TimeSpan.FromHours(hours);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      _logger.LogInformation("Payroll Background Service started. Interval: {Interval}h", _interval.TotalHours);

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

      _logger.LogInformation("Payroll Background Service stopped.");
    }

    private async Task ExecuteWithRetryAsync(CancellationToken stoppingToken)
    {
      var now = DateTime.UtcNow;
      // Only run payroll calculation near end of month (day >= 28) or start of next month (day == 1)
      if (now.Day < 28 && now.Day != 1)
      {
        _logger.LogDebug("Skipping payroll — not end of month (day {Day}).", now.Day);
        return;
      }

      for (int attempt = 1; attempt <= MaxRetries; attempt++)
      {
        try
        {
          var targetMonth = now.Day == 1 ? now.AddMonths(-1) : now;
          var monthStr = targetMonth.Month.ToString("D2");
          var yearStr = targetMonth.Year.ToString();

          _logger.LogInformation("Auto-triggering Payroll Calculation for {Month}-{Year}...", monthStr, yearStr);

          using var scope = _scopeFactory.CreateScope();
          var payrollProcessingService = scope.ServiceProvider.GetRequiredService<IPayrollProcessingService>();
          int processedCount = await payrollProcessingService.CalculatePayrollAsync(monthStr, yearStr);

          _logger.LogInformation("Auto-Payroll completed: {Count} employees processed.", processedCount);
          return;
        }
        catch (Exception ex) when (attempt < MaxRetries)
        {
          _logger.LogWarning(ex, "Payroll attempt {Attempt}/{Max} failed. Retrying in {Delay}s...",
              attempt, MaxRetries, attempt * 10);
          await Task.Delay(TimeSpan.FromSeconds(attempt * 10), stoppingToken);
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, "Payroll calculation failed after {Max} attempts.", MaxRetries);
        }
      }
    }
  }
}
