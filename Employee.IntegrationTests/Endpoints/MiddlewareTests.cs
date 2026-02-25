using System.Net;

namespace Employee.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for API middleware pipeline.
/// Tests cross-cutting concerns: routing, error handling, and endpoint availability.
/// Note: Auth endpoints have rate limiting (10 req/min), so some assertions
/// accept 429 (TooManyRequests) as a valid response.
/// </summary>
[Collection("Api")]
public class MiddlewareTests
{
  private readonly HttpClient _client;

  public MiddlewareTests(EmployeeApiFactory factory)
  {
    _client = factory.CreateClient();
  }

  // ============================================================
  // ROUTING TESTS
  // ============================================================

  [Fact]
  public async Task NonExistentEndpoint_Returns404()
  {
    // Act
    var response = await _client.GetAsync("/api/this-does-not-exist");

    // Assert
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
  }

  [Theory]
  [InlineData("/api/auth/login")]
  [InlineData("/api/auth/forgot-password")]
  [InlineData("/api/auth/reset-password")]
  [InlineData("/api/auth/refresh-token")]
  public async Task PublicAuthEndpoints_AcceptPost(string endpoint)
  {
    // Act — POST with empty JSON body
    var response = await _client.PostAsJsonAsync(endpoint, new { });

    // Assert — Should NOT be 404 or 405
    // Note: 429 (TooManyRequests) is acceptable — means the rate limiter is working
    Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    Assert.NotEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode);
  }

  [Theory]
  [InlineData("/api/auth/users")]
  [InlineData("/api/auth/roles")]
  public async Task ProtectedGetEndpoints_ReturnUnauthorizedOr429(string endpoint)
  {
    // Act
    var response = await _client.GetAsync(endpoint);

    // Assert — Should return 401 (route exists, needs auth) or 429 (rate limited)
    Assert.True(
      response.StatusCode == HttpStatusCode.Unauthorized ||
      response.StatusCode == (HttpStatusCode)429,
      $"Expected 401 or 429, got {(int)response.StatusCode}");
  }

  // ============================================================
  // ERROR HANDLING TESTS
  // ============================================================

  [Fact]
  public async Task InvalidJsonContent_Returns400Or429()
  {
    // Arrange
    var content = new StringContent(
      "not-valid-json!!!",
      System.Text.Encoding.UTF8,
      "application/json");

    // Act
    var response = await _client.PostAsync("/api/auth/login", content);

    // Assert — 400 (bad request) or 429 (rate limited by auth policy)
    Assert.True(
      response.StatusCode == HttpStatusCode.BadRequest ||
      response.StatusCode == (HttpStatusCode)429,
      $"Expected 400 or 429, got {(int)response.StatusCode}");
  }

  [Fact]
  public async Task GetAuth_WrongMethod_ReturnsMethodNotAllowedOr404Or429()
  {
    // Act — GET on a POST-only endpoint
    var response = await _client.GetAsync("/api/auth/login");

    // Assert — 405, 404, or 429 (rate limited)
    Assert.True(
      response.StatusCode == HttpStatusCode.MethodNotAllowed ||
      response.StatusCode == HttpStatusCode.NotFound ||
      response.StatusCode == (HttpStatusCode)429,
      $"Expected 405/404/429, got {(int)response.StatusCode}");
  }
}
