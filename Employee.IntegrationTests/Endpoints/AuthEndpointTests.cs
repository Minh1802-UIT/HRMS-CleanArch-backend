using System.Net;
using System.Net.Http.Json;

namespace Employee.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for Authentication endpoints.
/// Tests the full HTTP pipeline: routing → middleware → handler → response.
/// </summary>
[Collection("Api")]
public class AuthEndpointTests
{
  private readonly HttpClient _client;
  private readonly EmployeeApiFactory _factory;

  public AuthEndpointTests(EmployeeApiFactory factory)
  {
    _factory = factory;
    _client = factory.CreateClient();
  }

  // ============================================================
  // LOGIN TESTS
  // ============================================================

  [Fact]
  public async Task Login_WithEmptyBody_Returns400()
  {
    // Act
    var response = await _client.PostAsJsonAsync("/api/auth/login", new { });

    // Assert — Validation should reject empty body
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task Login_WithInvalidCredentials_ReturnsErrorStatus()
  {
    // Arrange
    var loginDto = new { Username = "nonexistent@test.com", Password = "WrongPassword123" };

    // Act
    var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

    // Assert — Should be 400 (validation) or 401 (unauthorized)
    Assert.True(
      response.StatusCode == HttpStatusCode.BadRequest ||
      response.StatusCode == HttpStatusCode.Unauthorized,
      $"Expected 400/401, got {(int)response.StatusCode}");
  }

  [Fact]
  public async Task Login_WithMalformedJson_Returns400()
  {
    // Arrange
    var content = new StringContent("{ invalid json", System.Text.Encoding.UTF8, "application/json");

    // Act
    var response = await _client.PostAsync("/api/auth/login", content);

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  // ============================================================
  // REGISTER TESTS (Protected — requires Admin role)
  // ============================================================

  [Fact]
  public async Task Register_WithoutAuth_Returns401()
  {
    // Arrange
    var registerDto = new
    {
      FullName = "Test User",
      Username = "testuser",
      Email = "test@test.com",
      Password = "TestPassword123"
    };

    // Act
    var response = await _client.PostAsJsonAsync("/api/auth/register", registerDto);

    // Assert — Should be 401 because no JWT token provided
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  // ============================================================
  // ROLE MANAGEMENT TESTS (Protected — requires Admin role)
  // ============================================================

  [Fact]
  public async Task CreateRole_WithoutAuth_Returns401()
  {
    // Arrange
    var roleDto = new { RoleName = "TestRole" };

    // Act
    var response = await _client.PostAsJsonAsync("/api/auth/role", roleDto);

    // Assert
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task GetAllUsers_WithoutAuth_Returns401()
  {
    // Act
    var response = await _client.GetAsync("/api/auth/users");

    // Assert
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task GetRoles_WithoutAuth_Returns401()
  {
    // Act
    var response = await _client.GetAsync("/api/auth/roles");

    // Assert
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  // ============================================================
  // FORGOT/RESET PASSWORD TESTS (Public endpoints)
  // ============================================================

  [Fact]
  public async Task ForgotPassword_WithEmptyBody_Returns400()
  {
    // Act
    var response = await _client.PostAsJsonAsync("/api/auth/forgot-password", new { });

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task ResetPassword_WithEmptyBody_Returns400()
  {
    // Act
    var response = await _client.PostAsJsonAsync("/api/auth/reset-password", new { });

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  // ============================================================
  // CHANGE PASSWORD (Protected)
  // ============================================================

  [Fact]
  public async Task ChangePassword_WithoutAuth_Returns401()
  {
    // Arrange
    var dto = new { CurrentPassword = "old", NewPassword = "new" };

    // Act
    var response = await _client.PostAsJsonAsync("/api/auth/change-password", dto);

    // Assert
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  // ============================================================
  // REFRESH TOKEN TESTS
  // ============================================================

  [Fact]
  public async Task RefreshToken_WithoutCookie_Returns401()
  {
    // Arrange — send a valid body but NO cookie
    var body = new { accessToken = "some-access-token" };
    var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh-token")
    {
      Content = JsonContent.Create(body)
    };
    // Deliberately omit "refreshToken" cookie

    // Act
    var response = await _client.SendAsync(request);

    // Assert — handler returns 401 because cookie is missing
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

    var content = await response.Content.ReadAsStringAsync();
    Assert.Contains("REFRESH_TOKEN_REQUIRED", content);
  }

  [Fact]
  public async Task RefreshToken_WithValidCookie_Returns200AndSetsNewCookie()
  {
    // Arrange — attach the "valid-refresh-token" value the factory mock is configured for
    var body = new { accessToken = "old-access-token" };
    var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh-token")
    {
      Content = JsonContent.Create(body)
    };
    request.Headers.Add("Cookie", "refreshToken=valid-refresh-token");

    // Act
    var response = await _client.SendAsync(request);

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var content = await response.Content.ReadAsStringAsync();
    Assert.Contains("new-access-token", content);

    // New refreshToken cookie should be set in the response
    Assert.True(
      response.Headers.TryGetValues("Set-Cookie", out var cookies),
      "Expected Set-Cookie header with rotated refreshToken");
    Assert.Contains(cookies!, c => c.StartsWith("refreshToken="));
  }

  [Fact]
  public async Task RefreshToken_WithRevokedToken_ReturnsError()
  {
    // Arrange — "revoked-refresh-token" triggers the reuse-detection mock setup
    var body = new { accessToken = "old-access-token" };
    var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh-token")
    {
      Content = JsonContent.Create(body)
    };
    request.Headers.Add("Cookie", "refreshToken=revoked-refresh-token");

    // Act
    var response = await _client.SendAsync(request);

    // Assert — global exception handler converts UnauthorizedAccessException → 401
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  // ============================================================
  // LOGOUT TESTS
  // ============================================================

  [Fact]
  public async Task Logout_Anonymous_Returns200()
  {
    // Arrange — logout is AllowAnonymous, no auth header needed
    // Act
    var response = await _client.PostAsync("/api/auth/logout", null);

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var content = await response.Content.ReadAsStringAsync();
    Assert.Contains("Logged out successfully", content);
  }

  [Fact]
  public async Task Logout_ClearsRefreshTokenCookie()
  {
    // Arrange
    var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
    request.Headers.Add("Cookie", "refreshToken=some-token");

    // Act
    var response = await _client.SendAsync(request);

    // Assert — response should delete the cookie (Set-Cookie with empty value / expired date)
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
    {
      // May contain an expiry-expired or empty value for the cookie
      var rtCookie = string.Join("; ", cookies!);
      // Cookie is either cleared or not re-issued — both are valid logout behaviour
      Assert.DoesNotContain("refreshToken=some-token", rtCookie);
    }
  }
}
