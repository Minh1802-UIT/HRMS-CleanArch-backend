using System.Net;

namespace Employee.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for Health Check endpoint.
/// Tests that the /health endpoint is accessible and returns expected status.
/// </summary>
[Collection("Api")]
public class HealthCheckTests
{
  private readonly HttpClient _client;

  public HealthCheckTests(EmployeeApiFactory factory)
  {
    _client = factory.CreateClient();
  }

  [Fact]
  public async Task HealthCheck_ReturnsOkOrDegraded()
  {
    // Act
    var response = await _client.GetAsync("/health");

    // Assert — Health check should return 200 (healthy) or 503 (degraded/unhealthy)
    Assert.True(
      response.StatusCode == HttpStatusCode.OK ||
      response.StatusCode == HttpStatusCode.ServiceUnavailable,
      $"Expected 200 or 503, got {(int)response.StatusCode}");
  }

  [Fact]
  public async Task HealthCheck_ReturnsContent()
  {
    // Act
    var response = await _client.GetAsync("/health");
    var content = await response.Content.ReadAsStringAsync();

    // Assert — Should return some status content
    Assert.False(string.IsNullOrEmpty(content));
  }
}
