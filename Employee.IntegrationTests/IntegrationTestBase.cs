namespace Employee.IntegrationTests;

/// <summary>
/// Base class for all integration test classes that use the "Api" collection.
/// Automatically skips all tests if Docker is not available (e.g., on developer machines
/// without Docker Desktop, or in CI environments without Docker).
///
/// Before each test: drops all collections (Linux MongoDB is case-sensitive; this clears
/// stray PascalCase collections) then starts the API host so startup seeding runs on a
/// clean database.
/// </summary>
public abstract class IntegrationTestBase : IAsyncLifetime, IDisposable
{
  protected readonly IntegrationTestFixture Fixture;
  protected readonly EmployeeApiFactory Factory;
  protected HttpClient Client { get; private set; } = null!;

  protected IntegrationTestBase(IntegrationTestFixture fixture)
  {
    Fixture = fixture;

    // Skip all tests if Docker was not available during fixture initialization.
    // xUnit will display this skip reason for every test in the class.
    if (!IntegrationTestFixture.IsDockerAvailable)
      throw new SkipTestException(
          "Docker is not available. Start Docker Desktop and re-run tests, " +
          "or run only unit tests when Docker is unavailable.");

    Factory = new EmployeeApiFactory(fixture);
  }

  /// <summary>Resets the database between tests for isolation.</summary>
  protected async Task ResetDatabaseAsync()
  {
    await Fixture.ResetDatabaseAsync();
  }

  public virtual async Task InitializeAsync()
  {
    await Fixture.ResetDatabaseAsync();
    Client = Factory.CreateClient();
  }

  public virtual Task DisposeAsync() => Task.CompletedTask;

  public void Dispose()
  {
    Client?.Dispose();
    Factory.Dispose();
  }
}

/// <summary>
/// Thrown by <see cref="IntegrationTestBase"/> constructor when Docker is unavailable.
/// xUnit catches this and marks all tests in the class as skipped.
/// </summary>
public class SkipTestException : Exception
{
  public SkipTestException(string message) : base(message) { }
}
