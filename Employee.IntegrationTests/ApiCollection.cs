namespace Employee.IntegrationTests;

/// <summary>
/// xUnit Collection Fixture — ensures all integration test classes share
/// a single WebApplicationFactory instance. This prevents duplicate
/// MongoDB BsonClassMap registration (global static) between test classes.
/// </summary>
[CollectionDefinition("Api")]
public class ApiCollection : ICollectionFixture<EmployeeApiFactory>
{
  // No code needed — xUnit uses this class to share the factory
}
