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
    /// When to run the morning simulation. Default: 01:05 UTC = 08:05 ICT.
    /// Format: "HH:mm" UTC.
    /// </summary>
    private readonly TimeSpan _morningSimulationTime;

    /// <summary>
    /// When to run the evening simulation (CheckOut phase). Default: 11:05 UTC = 18:05 ICT.
    /// Format: "HH:mm" UTC.
    /// </summary>
    private readonly TimeSpan _eveningSimulationTime;

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

        var morningStr = configuration["BackgroundJobs:MorningSimulationTimeUtc"] ?? "01:05";
        var eveningStr = configuration["BackgroundJobs:EveningSimulationTimeUtc"] ?? "11:05";
        _morningSimulationTime = TimeSpan.Parse(morningStr);
        _eveningSimulationTime = TimeSpan.Parse(eveningStr);

        _logger.LogInformation(
            "SimulationBackgroundService started. Morning scheduled at {MorningTime} UTC. Evening scheduled at {EveningTime} UTC. " +
            "Check interval: {Interval} min.",
            _morningSimulationTime,
            _eveningSimulationTime,
            _checkInterval.TotalMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var timer = new PeriodicTimer(_checkInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            if (stoppingToken.IsCancellationRequested) break;

            var nowUtc = DateTime.UtcNow;
            var runMorning = nowUtc.TimeOfDay >= _morningSimulationTime
                        && nowUtc.TimeOfDay < _morningSimulationTime + _checkInterval;

            if (runMorning)
            {
                await RunMorningSimulationAsync(stoppingToken);
            }

            var runEvening = nowUtc.TimeOfDay >= _eveningSimulationTime
                        && nowUtc.TimeOfDay < _eveningSimulationTime + _checkInterval;

            if (runEvening)
            {
                await RunEveningSimulationAsync(stoppingToken);
            }
        }
    }

    private async Task RunMorningSimulationAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ISimulationService>();

            _logger.LogInformation("[Simulation] Starting morning simulation run...");
            var result = await service.RunMorningSimulationAsync(stoppingToken);

            _logger.LogInformation(
                "[Simulation] Morning completed: {Success} ok, {Failed} failed, {Skipped} skipped, {DurationMs}ms",
                result.SuccessCount, result.FailureCount, result.SkippedCount, result.DurationMs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Simulation] Morning simulation run failed.");
        }
    }

    private async Task RunEveningSimulationAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ISimulationService>();

            _logger.LogInformation("[Simulation] Starting evening simulation run...");
            var result = await service.RunEveningSimulationAsync(stoppingToken);

            _logger.LogInformation(
                "[Simulation] Evening completed: {Success} ok, {Failed} failed, {Skipped} skipped, {DurationMs}ms",
                result.SuccessCount, result.FailureCount, result.SkippedCount, result.DurationMs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Simulation] Evening simulation run failed.");
        }
    }
}
