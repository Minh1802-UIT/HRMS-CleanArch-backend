using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Testcontainers.MongoDb;
using Testcontainers.Redis;

namespace Employee.IntegrationTests;

/// <summary>
/// Shared singleton that owns the MongoDB and Redis Testcontainers for the entire
/// test session. All integration test classes share this via <see cref="ApiCollection"/>.
///
/// Benefits:
/// - Containers start once → ~30-60s savings per test class vs. per-class startup.
/// - Real MongoDB driver used → tests exercise the same serialization/deserialization
///   pipeline as production (no in-memory mock gaps).
/// - No local MongoDB/Redis installation required → CI-friendly.
///
/// Drop-all behavior: <see cref="IntegrationTestBase.InitializeAsync"/> calls
/// <see cref="ResetDatabaseAsync"/> before each test so collections match app expectations
/// on case-sensitive hosts (e.g. Linux CI) and no stale data triggers flaky attendance rules.
///
/// xUnit integration: xUnit requires collection fixtures to have a public parameterless
/// constructor. Initialization is triggered lazily on first property access.
/// If Docker is unavailable, tests are skipped with a clear message.
/// </summary>
public sealed class IntegrationTestFixture : IAsyncDisposable
{
  private static readonly SemaphoreSlim _initLock = new(initialCount: 1, maxCount: 1);
  private static bool _initialized;
  private static bool _dockerAvailable;

  private readonly MongoDbContainer? _mongoContainer;
  private readonly RedisContainer? _redisContainer;
  private readonly string _databaseName;
  private bool _disposed;

  public string MongoConnectionString { get; }
  public string RedisConnectionString { get; }
  public string DatabaseName => _databaseName;
  public IMongoDatabase Database { get; }

  /// <summary>
  /// Returns true if Docker is running and containers were started successfully.
  /// Tests should check this and skip if Docker is not available.
  /// </summary>
  public static bool IsDockerAvailable => _dockerAvailable;

  /// <summary>
  /// xUnit requires a parameterless constructor.
  /// Initialization is triggered lazily on first property access.
  /// </summary>
  public IntegrationTestFixture()
  {
    // Try lazy initialization. If Docker is unavailable, fall back to localhost defaults.
    LazyInitSync();
    _mongoContainer = _lazyContainer;
    _redisContainer = _lazyRedis;
    _databaseName = _lazyDatabaseName ?? $"hrms_it_fallback_{Guid.NewGuid():N}";
    MongoConnectionString = _lazyMongoConn ?? "mongodb://localhost:27017";
    RedisConnectionString = _lazyRedisConn ?? "localhost:6379";
    Database = _lazyDatabase ?? new MongoClient(MongoConnectionString).GetDatabase(_databaseName);
  }

  // Static lazy fields — set once during first LazyInitSync() call.
  private static MongoDbContainer? _lazyContainer;
  private static RedisContainer? _lazyRedis;
  private static string? _lazyMongoConn;
  private static string? _lazyRedisConn;
  private static string? _lazyDatabaseName;
  private static IMongoDatabase? _lazyDatabase;

  /// <summary>
  /// Starts both containers synchronously (blocking).
  /// Safe to call multiple times — subsequent calls return immediately.
  /// </summary>
  private static void LazyInitSync()
  {
    if (_initialized) return;

    bool lockTaken = _initLock.Wait(0);
    if (!lockTaken) return; // Another thread is initializing; safe to skip.
    try
    {
      if (_initialized) return;

      Console.WriteLine("[IntegrationTestFixture] Checking Docker availability...");

      try
      {
        // Build containers (Testcontainers 3.x Build() is fast, no I/O).
        var mongoContainer = new MongoDbBuilder()
            .WithImage("mongo:7.0")
            .WithCleanUp(true)
            .WithLabel("org.hrms", "integration-test")
            .Build();

        var redisContainer = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .WithCleanUp(true)
            .WithLabel("org.hrms", "integration-test")
            .Build();

        // Start containers synchronously via blocking await.
        mongoContainer.StartAsync().GetAwaiter().GetResult();
        redisContainer.StartAsync().GetAwaiter().GetResult();

        _lazyMongoConn = mongoContainer.GetConnectionString();
        _lazyRedisConn = redisContainer.GetConnectionString();
        _lazyDatabaseName = $"hrms_it_{Environment.ProcessId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}";

        var client = new MongoClient(_lazyMongoConn);
        _lazyDatabase = client.GetDatabase(_lazyDatabaseName);

        // Ensure BsonClassMaps are registered (same as Program.cs).
        Employee.Infrastructure.data.Configurations.MongoClassMapConfig.Configure();

        _lazyContainer = mongoContainer;
        _lazyRedis = redisContainer;
        _dockerAvailable = true;

        Console.WriteLine($"[IntegrationTestFixture] Docker available — MongoDB: {_lazyMongoConn}");
        Console.WriteLine($"[IntegrationTestFixture] Redis:    {_lazyRedisConn}");
        Console.WriteLine($"[IntegrationTestFixture] Database: {_lazyDatabaseName}");
      }
      catch (Exception ex)
      {
        _dockerAvailable = false;
        Console.WriteLine($"[IntegrationTestFixture] Docker is not available: {ex.Message}");
        Console.WriteLine("[IntegrationTestFixture] Integration tests will be skipped.");
        Console.WriteLine("[IntegrationTestFixture] To run integration tests, ensure Docker is running.");
      }

      _initialized = true;
    }
    finally
    {
      if (lockTaken) _initLock.Release();
    }
  }

  /// <summary>
  /// Starts both containers asynchronously (non-blocking alternative).
  /// </summary>
  public static async Task EnsureInitializedAsync()
  {
    if (_initialized) return;
    await _initLock.WaitAsync();
    try
    {
      if (_initialized) return;
      LazyInitSync();
    }
    finally
    {
      _initLock.Release();
    }
  }

  /// <summary>
  /// Drops ALL collections in the test database. Call this between tests to guarantee
  /// test isolation even when a previous test fails and doesn't clean up.
  /// </summary>
  public async Task ResetDatabaseAsync()
  {
    var client = new MongoClient(MongoConnectionString);
    var db = client.GetDatabase(_databaseName);

    try
    {
      var collections = await db.ListCollectionNamesAsync();
      var names = await collections.ToListAsync();
      foreach (var name in names)
      {
        await db.DropCollectionAsync(name);
      }
    }
    catch (MongoCommandException ex) when (ex.Code == 26 || ex.Message.Contains("NamespaceNotFound"))
    {
      // Database was already dropped — safe to ignore.
    }
  }

  public async ValueTask DisposeAsync()
  {
    if (_disposed) return;
    _disposed = true;

    if (_mongoContainer != null && _redisContainer != null)
    {
      Console.WriteLine("[IntegrationTestFixture] Stopping containers...");
      await _mongoContainer.DisposeAsync();
      await _redisContainer.DisposeAsync();
      Console.WriteLine("[IntegrationTestFixture] Containers stopped.");
    }
  }
}
