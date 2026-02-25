using System.Net;

namespace Employee.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for Authentication endpoints.
/// Tests the full HTTP pipeline: routing → middleware → handler → response.
/// </summary>
[Collection("Api")]
public class AuthEndpointTests
{
  private readonly HttpClient _client;

  public AuthEndpointTests(EmployeeApiFactory factory)
  {
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
}
