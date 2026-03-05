using System.Text.Json.Serialization;

namespace Employee.Application.Features.Auth.Dtos
{
  // 1. REGISTER
  public class RegisterDto
  {
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;

    // Optional — admin accounts may not be linked to an employee record
    public string? EmployeeId { get; set; }

    // If true, the user must change their password on first login
    public bool MustChangePassword { get; set; } = false;
  }

  // 2. LOGIN
  public class LoginDto
  {
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
  }

  // 3. CREATE ROLE
  public class CreateRoleDto
  {
    // Only letters and underscores are allowed (e.g. "HR_MANAGER", "ADMIN")
    public string RoleName { get; set; } = string.Empty;
  }

  // 4. ASSIGN ROLE
  public class AssignRoleDto
  {
    public string Username { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
  }

  // 5. REFRESH TOKEN (body-based, legacy)
  public class RefreshTokenDto
  {
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
  }

  // 5b. Cookie-based refresh — refresh token comes from httpOnly cookie
  public class RefreshAccessTokenDto
  {
    public string AccessToken { get; set; } = string.Empty;
  }

  public class LoginResponseDto
  {
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; }

    public UserDto User { get; set; } = new();
  }

  /// <summary>
  /// The exact shape returned to the client from POST /api/auth/login.
  /// RefreshToken is intentionally excluded — it lives in an httpOnly cookie only.
  /// [JsonPropertyName] guarantees camelCase regardless of serializer settings.
  /// </summary>
  public class LoginSuccessDto
  {
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("tokenType")]
    public string TokenType { get; set; } = "Bearer";

    [JsonPropertyName("expiresIn")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("user")]
    public UserDto User { get; set; } = new();
  }

  /// <summary>
  /// The exact shape returned to the client from POST /api/auth/refresh-token.
  /// </summary>
  public class RefreshSuccessDto
  {
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("tokenType")]
    public string TokenType { get; set; } = "Bearer";

    [JsonPropertyName("expiresIn")]
    public int ExpiresIn { get; set; }
  }

  // FORGOT PASSWORD
  public class ForgotPasswordDto
  {
    public string Email { get; set; } = string.Empty;
  }

  // RESET PASSWORD
  public class ResetPasswordDto
  {
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
  }
}