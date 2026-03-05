using Employee.Application.Common.Interfaces.Organization.IService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Employee.Infrastructure.BackgroundServices
{
  /// <summary>
  /// Background service that sweeps unprocessed <c>RawAttendanceLog</c> records and
  /// processes them into <c>AttendanceBucket</c> documents on a fixed schedule.
  ///
  /// Architectural note: <c>CheckInHandler</c> no longer calls the processing service
  /// inline. The API returns immediately after persisting the raw punch; this job picks
  /// it up within the configured interval (default: 5 minutes). This decouples the
  /// write-path latency from the (potentially slow) bucket-update logic and prevents
  /// any processing failure from failing the check-in request itself.
  ///
  /// Configuration (appsettings.json):
  /// <code>
  ///   "BackgroundJobs": {
  ///     "AttendanceProcessingIntervalMinutes": 5
  ///   }
  /// </code>
  /// </summary>
  public class AttendanceProcessingBackgroundJob : BackgroundService
  {
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AttendanceProcessingBackgroundJob> _logger;
    private readonly TimeSpan _interval;
    private const int MaxRetries = 3;

    public AttendanceProcessingBackgroundJob(
        IServiceScopeFactory scopeFactory,
        ILogger<AttendanceProcessingBackgroundJob> logger,
        IConfiguration configuration)
    {
      _scopeFactory = scopeFactory;
      _logger       = logger;
      var minutes   = configuration.GetValue<int>(
          "BackgroundJobs:AttendanceProcessingIntervalMinutes", defaultValue: 5);
      _interval     = TimeSpan.FromMinutes(minutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      _logger.LogInformation(
          "AttendanceProcessingBackgroundJob started. Sweep interval: {Interval} min",
          _interval.TotalMinutes);

      // Run one sweep immediately on startup so that any logs persisted while the
      // service was down are processed without waiting a full interval.
      await SweepWithRetryAsync(stoppingToken);

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

        await SweepWithRetryAsync(stoppingToken);
      }

      _logger.LogInformation("AttendanceProcessingBackgroundJob stopped.");
    }

    // -------------------------------------------------------------------------
    // Internal sweep with retry / back-off
    // -------------------------------------------------------------------------
    private async Task SweepWithRetryAsync(CancellationToken stoppingToken)
    {
      for (int attempt = 1; attempt <= MaxRetries; attempt++)
      {
        try
        {
          // IAttendanceProcessingService is Scoped — must be resolved inside a scope.
          using var scope = _scopeFactory.CreateScope();
          var processingService = scope.ServiceProvider
              .GetRequiredService<IAttendanceProcessingService>();

          var result = await processingService.ProcessRawLogsAsync();
          _logger.LogDebug("AttendanceProcessingBackgroundJob sweep result: {Result}", result);
          return; // success — exit retry loop
        }
        catch (Exception ex) when (attempt < MaxRetries && !stoppingToken.IsCancellationRequested)
        {
          var delay = TimeSpan.FromSeconds(attempt * 15); // 15s, 30s back-off
          _logger.LogWarning(ex,
              "AttendanceProcessingBackgroundJob sweep attempt {Attempt}/{Max} failed. " +
              "Retrying in {Delay}s. Error: {Error}",
              attempt, MaxRetries, delay.TotalSeconds, ex.Message);
          await Task.Delay(delay, stoppingToken);
        }
        catch (Exception ex)
        {
          _logger.LogError(ex,
              "AttendanceProcessingBackgroundJob sweep failed after {Max} attempts. Error: {Error}",
              MaxRetries, ex.Message);
          return; // give up until next scheduled sweep
        }
      }
    }
  }
}
