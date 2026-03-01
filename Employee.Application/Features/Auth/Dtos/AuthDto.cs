using System.Text.Json.Serialization;

namespace Employee.Application.Features.Auth.Dtos
{
  // 1. REGISTER (Ðang ký)
  public class RegisterDto
  {
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    // Có th? m? comment dòng du?i n?u mu?n b?t bu?c m?t kh?u m?nh (Ch? hoa, thu?ng, s?, ký t? d?c bi?t)
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;

    // ID nhân viên (Optional - Vì có th? t?o user Admin không g?n v?i nhân viên nào)
    public string? EmployeeId { get; set; }

    // Flag: if true, the user is required to change their password on first login
    public bool MustChangePassword { get; set; } = false;
  }

  // 2. LOGIN (Ðang nh?p)
  public class LoginDto
  {
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
  }

  // 3. CREATE ROLE (T?o quy?n)
  public class CreateRoleDto
  {
    // Ch? cho phép ch? cái và g?ch du?i (VD: "HR_MANAGER", "ADMIN") d? tránh l?i h? th?ng
    public string RoleName { get; set; } = string.Empty;
  }

  // 4. ASSIGN ROLE (Gán quy?n)
  public class AssignRoleDto
  {
    public string Username { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
  }

  // 5. (B? sung) REFRESH TOKEN
  // DTO này r?t quan tr?ng d? l?y token m?i khi token cu h?t h?n
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
    public string RefreshToken { get; set; } = string.Empty; // NEW-2
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; }

    public UserDto User { get; set; } = new();
  }

  /// <summary>
  /// The EXACT shape returned to the client from POST /api/auth/login.
  /// RefreshToken is intentionally excluded — it lives in an httpOnly cookie only.
  /// Using [JsonPropertyName] guarantees camelCase regardless of serializer settings.
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
  /// The EXACT shape returned to the client from POST /api/auth/refresh-token.
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

  // NEW-3: Forgot Password
  public class ForgotPasswordDto
  {
    public string Email { get; set; } = string.Empty;
  }

  // NEW-3: Reset Password
  public class ResetPasswordDto
  {
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
  }
}