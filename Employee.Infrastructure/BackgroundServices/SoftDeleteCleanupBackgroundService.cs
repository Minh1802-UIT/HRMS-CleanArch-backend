using Employee.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Employee.Infrastructure.BackgroundServices
{
  /// <summary>
  /// Nightly background job that hard-deletes soft-deleted documents
  /// older than <see cref="RetentionDays"/> from all auditable collections.
  ///
  /// Why this matters:
  ///   Soft-deleted documents are still stored in MongoDB and count against
  ///   index size, collection scans, and storage costs.  After 90 days the
  ///   records are unlikely to be needed for recovery, so they are purged to
  ///   keep index sizes small and query performance high.
  ///
  /// Cadence: once every 24 hours, with a 5-minute startup delay to avoid
  ///   racing against pod initialisation.
  /// </summary>
  public class SoftDeleteCleanupBackgroundService : BackgroundService
  {
    private const int RetentionDays      = 90;
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan RunInterval   = TimeSpan.FromHours(24);

    // Collections that participate in soft-delete cleanup.
    // Add more collection names here as the domain grows.
    private static readonly string[] TargetCollections =
    [
      "employees",
      "contracts",
      "leave_requests",
      "leave_allocations",
      "attendance_buckets",
      "payrolls",
      "shifts"
    ];

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SoftDeleteCleanupBackgroundService> _logger;

    public SoftDeleteCleanupBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<SoftDeleteCleanupBackgroundService> logger)
    {
      _scopeFactory = scopeFactory;
      _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      // Delay at startup so the app is fully initialised before first run
      await Task.Delay(InitialDelay, stoppingToken);

      while (!stoppingToken.IsCancellationRequested)
      {
        try
        {
          await RunCleanupAsync(stoppingToken);
        }
        catch (OperationCanceledException) { /* graceful shutdown */ }
        catch (Exception ex)
        {
          _logger.LogError(ex, "[SoftDeleteCleanup] Unexpected error during cleanup cycle.");
        }

        await Task.Delay(RunInterval, stoppingToken);
      }
    }

    private async Task RunCleanupAsync(CancellationToken cancellationToken)
    {
      var cutoff = DateTime.UtcNow.AddDays(-RetentionDays);
      _logger.LogInformation(
          "[SoftDeleteCleanup] Starting. Purging records with IsDeleted=true and UpdatedAt < {Cutoff:O}",
          cutoff);

      long totalDeleted = 0;

      using var scope   = _scopeFactory.CreateScope();
      var context       = scope.ServiceProvider.GetRequiredService<IMongoContext>();

      // Filter: IsDeleted == true AND UpdatedAt < cutoff
      // (UpdatedAt is set by BaseRepository.DeleteAsync when the record is soft-deleted)
      var filter = Builders<BsonDocument>.Filter.And(
          Builders<BsonDocument>.Filter.Eq("IsDeleted", true),
          Builders<BsonDocument>.Filter.Lt("UpdatedAt", cutoff)
      );

      foreach (var collectionName in TargetCollections)
      {
        try
        {
          var collection = context.GetCollection<BsonDocument>(collectionName);
          var result     = await collection.DeleteManyAsync(filter, cancellationToken);

          if (result.DeletedCount > 0)
          {
            totalDeleted += result.DeletedCount;
            _logger.LogInformation(
                "[SoftDeleteCleanup] '{Collection}': purged {Count} record(s).",
                collectionName, result.DeletedCount);
          }
        }
        catch (Exception ex)
        {
          // Log per-collection errors so a single failing collection doesn't abort the whole run
          _logger.LogError(ex,
              "[SoftDeleteCleanup] Failed to purge collection '{Collection}'.", collectionName);
        }
      }

      _logger.LogInformation(
          "[SoftDeleteCleanup] Completed. Total hard-deleted: {Total}.", totalDeleted);
    }
  }
}
