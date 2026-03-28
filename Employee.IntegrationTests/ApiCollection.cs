namespace Employee.IntegrationTests;

/// <summary>
/// xUnit Collection Fixture — ensures a single shared <see cref="IntegrationTestFixture"/>
/// (and therefore a single set of Testcontainers) for the entire test session.
/// This prevents duplicate MongoDB BsonClassMap registrations between test classes and
/// shares the container startup cost across all test classes.
///
/// If Docker is unavailable, all tests in this collection are skipped.
/// </summary>
[CollectionDefinition("Api")]
public class ApiCollection : ICollectionFixture<IntegrationTestFixture>
{
  // xUnit uses this class solely for the fixture type declaration.
  // The actual fixture instance is created lazily by xUnit's collection system.
  // LazyInitSync() runs in the IntegrationTestFixture constructor and will set
  // IsDockerAvailable accordingly.
}
