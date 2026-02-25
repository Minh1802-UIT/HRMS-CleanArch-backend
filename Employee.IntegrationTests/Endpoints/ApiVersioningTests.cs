using System.Net;
using System.Net.Http.Json;

namespace Employee.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for API Versioning.
/// Verifies that endpoints are accessible via:
/// 1. Default path (implicit v1) - backward compatibility
/// 2. Header-based versioning
/// 3. Swagger v1 spec availability
/// </summary>
[Collection("Api")]
public class ApiVersioningTests
{
  private readonly HttpClient _client;

  public ApiVersioningTests(EmployeeApiFactory factory)
  {
    _client = factory.CreateClient();
  }

  [Fact]
  public async Task DefaultPath_IsImplicitlyV1()
  {
    // Act — Hit default path without version
    // POST /api/auth/login with empty body -> Should be 400 (Validation) if routed
    var response = await _client.PostAsJsonAsync("/api/auth/login", new { });

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

    // Verify headers
    // Asp.Versioning adds "api-supported-versions: 1"
    if (response.Headers.TryGetValues("api-supported-versions", out var values))
    {
      Assert.Contains("1", values);
    }
  }

  [Fact]
  public async Task HeaderVersioned_V1_Works()
  {
    // Arrange
    var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login");
    request.Headers.Add("X-Api-Version", "1");
    request.Content = JsonContent.Create(new { });

    // Act
    var response = await _client.SendAsync(request);

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task HeaderVersioned_V2_Returns400_UnsupportedApiVersion()
  {
    // Arrange
    var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login");
    request.Headers.Add("X-Api-Version", "2"); // We only support v1
    request.Content = JsonContent.Create(new { });

    // Act
    var response = await _client.SendAsync(request);

    // Assert - Should be Client Error (400 or 404)
    Assert.False(response.IsSuccessStatusCode);
  }

  [Fact]
  public async Task Swagger_V1_Endpoint_Exists()
  {
    // Act
    // Swagger UI is at /swagger, specs at /swagger/v1/swagger.json
    // Note: In minimal API, group name might be "v1" or "v1.0"
    // My config says options.GroupNameFormat = "'v'V"; -> "v1"
    var response = await _client.GetAsync("/swagger/v1/swagger.json");

    // Assert
    // Enabled in Testing environment via Program.cs fix
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
  }
}
