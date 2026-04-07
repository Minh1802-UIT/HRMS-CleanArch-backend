using Hangfire;
using Employee.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Employee.Infrastructure.BackgroundServices;

/// <summary>
/// Registers all Hangfire recurring jobs for the Simulation Engine.
/// Runs daily at 08:05 AM ICT (01:05 UTC) — after the normal check-in window opens.
///
/// Hangfire handles persistence, retries, and dashboard visibility automatically.
/// Jobs survive app restarts because they're stored in Redis.
/// </summary>
public static class SimulationBackgroundJobs
{
    public const string MorningSimulationJobId = "simulation.morning";
    public const string EveningSimulationJobId = "simulation.evening";

    /// <summary>
    /// Cron: every day at 01:05 UTC = 08:05 ICT.
    /// </summary>
    public const string MorningCron = "5 1 * * *";

    /// <summary>
    /// Cron: every day at 11:05 UTC = 18:05 ICT.
    /// </summary>
    public const string EveningCron = "5 11 * * *";

    /// <summary>
    /// Cron: first day of every month at 00:30 UTC = 07:30 ICT.
    /// Bot provisioning ensures every active employee has a simulation bot.
    /// </summary>
    public const string MonthlyProvisioningCron = "30 0 1 * *";
}

/// <summary>
/// Background service that bootstraps Hangfire recurring jobs on startup.
/// Safe to use even if Hangfire was unavailable during initial app startup.
/// Resolves IRecurringJobManager from DI to register jobs.
/// </summary>
public class SimulationBootstrapService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SimulationBootstrapService> _logger;

    public SimulationBootstrapService(
        IServiceScopeFactory scopeFactory,
        ILogger<SimulationBootstrapService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
            var service = scope.ServiceProvider.GetRequiredService<ISimulationService>();

            // ── Morning Simulation (08:05 ICT) ─────────────────────────────────
            recurringJobManager.AddOrUpdate<ISimulationService>(
                SimulationBackgroundJobs.MorningSimulationJobId,
                s => s.RunMorningSimulationAsync(default),
                SimulationBackgroundJobs.MorningCron,
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.Utc,
                    MisfireHandling = MisfireHandlingMode.Relaxed
                });

            Console.WriteLine($"[SimulationEngine] Registered morning simulation job '{SimulationBackgroundJobs.MorningSimulationJobId}' with cron '{SimulationBackgroundJobs.MorningCron}' (08:05 ICT)");

            // ── Evening Simulation (18:05 ICT) ─────────────────────────────────
            recurringJobManager.AddOrUpdate<ISimulationService>(
                SimulationBackgroundJobs.EveningSimulationJobId,
                s => s.RunEveningSimulationAsync(default),
                SimulationBackgroundJobs.EveningCron,
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.Utc,
                    MisfireHandling = MisfireHandlingMode.Relaxed
                });

            Console.WriteLine($"[SimulationEngine] Registered evening simulation job '{SimulationBackgroundJobs.EveningSimulationJobId}' with cron '{SimulationBackgroundJobs.EveningCron}' (18:05 ICT)");

            // ── Monthly Bot Provisioning ────────────────────────────────────
            recurringJobManager.AddOrUpdate<ISimulationService>(
                "simulation.monthly-provision",
                s => s.ProvisionBotsAsync(default),
                SimulationBackgroundJobs.MonthlyProvisioningCron,
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.Utc,
                    MisfireHandling = MisfireHandlingMode.Relaxed
                });

            Console.WriteLine($"[SimulationEngine] Registered monthly provisioning job with cron '{SimulationBackgroundJobs.MonthlyProvisioningCron}' (07:30 ICT on 1st of month)");
            _logger.LogInformation("Simulation engine Hangfire jobs bootstrapped successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to bootstrap simulation Hangfire jobs. " +
                "They will be registered when the next app restart occurs or can be triggered manually via the API.");
        }
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
