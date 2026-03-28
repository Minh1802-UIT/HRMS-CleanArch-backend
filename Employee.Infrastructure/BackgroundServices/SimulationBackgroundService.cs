using Employee.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Employee.Infrastructure.BackgroundServices;

/// <summary>
/// Drives the Simulation Engine using a BackgroundService + PeriodicTimer.
/// 
/// In Development: uses PeriodicTimer — no Redis/Hangfire needed.
/// In Production: Hangfire also runs (if Redis available) and uses this service
/// as the bootstrap for recurring jobs.
///
/// Either way, the simulation engine is powered by this BackgroundService
/// with configurable schedules via appsettings.json.
/// </summary>
public class SimulationBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SimulationBackgroundService> _logger;

    /// <summary>
    /// When to run the daily simulation. Default: 01:05 UTC = 08:05 ICT.
    /// Format: "HH:mm" UTC.
    /// </summary>
    private readonly TimeSpan _dailySimulationTime;

    /// <summary>
    /// How often to check if it's time for the daily run. Default: every 5 minutes.
    /// </summary>
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);

    public SimulationBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<SimulationBackgroundService> logger,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        var timeStr = configuration["BackgroundJobs:DailySimulationTimeUtc"] ?? "01:05";
        _dailySimulationTime = TimeSpan.Parse(timeStr);

        _logger.LogInformation(
            "SimulationBackgroundService started. Daily simulation scheduled at {Time} UTC ({IctTime} ICT). " +
            "Check interval: {Interval} min.",
            _dailySimulationTime,
            _dailySimulationTime + TimeSpan.FromHours(7),
            _checkInterval.TotalMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var timer = new PeriodicTimer(_checkInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            if (stoppingToken.IsCancellationRequested) break;

            var nowUtc = DateTime.UtcNow;
            var shouldRun = nowUtc.TimeOfDay >= _dailySimulationTime
                        && nowUtc.TimeOfDay < _dailySimulationTime + _checkInterval;

            if (shouldRun)
            {
                await RunSimulationAsync(stoppingToken);
            }
        }
    }

    private async Task RunSimulationAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ISimulationService>();

            _logger.LogInformation("[Simulation] Starting daily simulation run...");
            var result = await service.RunDailySimulationAsync(stoppingToken);

            _logger.LogInformation(
                "[Simulation] Completed: {Success} ok, {Failed} failed, {Skipped} skipped, {DurationMs}ms",
                result.SuccessCount, result.FailureCount, result.SkippedCount, result.DurationMs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Simulation] Daily simulation run failed.");
        }
    }
}
