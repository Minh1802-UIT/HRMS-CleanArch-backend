using Employee.Application.Features.HumanResource.Commands.Contracts;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Employee.Infrastructure.BackgroundServices
{
  /// <summary>
  /// Refactored: Background Service acts as a simple trigger.
  /// Business logic moved to MediatR Handler ExpireContractsHandler.
  /// </summary>
  public class ContractExpirationBackgroundService : BackgroundService
  {
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ContractExpirationBackgroundService> _logger;
    private readonly TimeSpan _interval;
    private const int MaxRetries = 2;

    public ContractExpirationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<ContractExpirationBackgroundService> logger,
        IConfiguration configuration)
    {
      _serviceProvider = serviceProvider;
      _logger = logger;
      var hours = configuration.GetValue<int>("BackgroundJobs:ContractExpirationIntervalHours", 24);
      _interval = TimeSpan.FromHours(hours);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      _logger.LogInformation("Contract Expiration Background Service started. Interval: {Interval}h", _interval.TotalHours);

      while (!stoppingToken.IsCancellationRequested)
      {
        await TryTriggerExpirationAsync(stoppingToken);

        try
        {
          await Task.Delay(_interval, stoppingToken);
        }
        catch (OperationCanceledException)
        {
          break;
        }
      }

      _logger.LogInformation("Contract Expiration Background Service stopped.");
    }

    private async Task TryTriggerExpirationAsync(CancellationToken stoppingToken)
    {
      for (int attempt = 1; attempt <= MaxRetries; attempt++)
      {
        try
        {
          using var scope = _serviceProvider.CreateScope();
          var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

          _logger.LogInformation("Triggering contract expiration check via MediatR...");
          var count = await mediator.Send(new ExpireContractsCommand(), stoppingToken);

          if (count > 0)
          {
            _logger.LogInformation("Contract expiration check completed. {Count} contracts processed.", count);
          }

          return;
        }
        catch (Exception ex) when (attempt < MaxRetries)
        {
          _logger.LogWarning(ex, "Trigger attempt {Attempt} failed. Retrying in 30s...", attempt);
          await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, "Failed to trigger contract expiration after {Max} attempts.", MaxRetries);
        }
      }
    }
  }
}
