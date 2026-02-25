using System.ComponentModel.DataAnnotations;

namespace Employee.Application.Features.Auth.Dtos
{
  // 1. REGISTER (Đăng ký)
  public class RegisterDto
  {
    [Required(ErrorMessage = "Username is required.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters.")]
    [RegularExpression(@"^[a-zA-Z0-9._-]+$", ErrorMessage = "Username can only contain letters, numbers, dots, underscores, and hyphens.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    [MaxLength(100, ErrorMessage = "Email must not exceed 100 characters.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
    // Có thể mở comment dòng dưới nếu muốn bắt buộc mật khẩu mạnh (Chữ hoa, thường, số, ký tự đặc biệt)
    // [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{6,}$", ErrorMessage = "Password must have at least 1 uppercase, 1 lowercase, 1 number, and 1 special character.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Full Name is required.")]
    [MaxLength(100, ErrorMessage = "Full Name must not exceed 100 characters.")]
    public string FullName { get; set; } = string.Empty;

    // ID nhân viên (Optional - Vì có thể tạo user Admin không gắn với nhân viên nào)
    [MaxLength(50, ErrorMessage = "Employee ID must not exceed 50 characters.")]
    public string? EmployeeId { get; set; }

    // Flag: if true, the user is required to change their password on first login
    public bool MustChangePassword { get; set; } = false;
  }

  // 2. LOGIN (Đăng nhập)
  public class LoginDto
  {
    [Required(ErrorMessage = "Username is required.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; set; } = string.Empty;
  }

  // 3. CREATE ROLE (Tạo quyền)
  public class CreateRoleDto
  {
    [Required(ErrorMessage = "Role Name is required.")]
    [MaxLength(20, ErrorMessage = "Role Name must not exceed 20 characters.")]
    // Chỉ cho phép chữ cái và gạch dưới (VD: "HR_MANAGER", "ADMIN") để tránh lỗi hệ thống
    [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Role Name can only contain letters, numbers and underscores.")]
    public string RoleName { get; set; } = string.Empty;
  }

  // 4. ASSIGN ROLE (Gán quyền)
  public class AssignRoleDto
  {
    [Required(ErrorMessage = "Username is required.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role Name is required.")]
    public string RoleName { get; set; } = string.Empty;
  }

  // 5. (Bổ sung) REFRESH TOKEN
  // DTO này rất quan trọng để lấy token mới khi token cũ hết hạn
  public class RefreshTokenDto
  {
    [Required(ErrorMessage = "Access Token is required.")]
    public string AccessToken { get; set; } = string.Empty;

    [Required(ErrorMessage = "Refresh Token is required.")]
    public string RefreshToken { get; set; } = string.Empty;
  }

  // 5b. Cookie-based refresh — refresh token comes from httpOnly cookie
  public class RefreshAccessTokenDto
  {
    [Required(ErrorMessage = "Access Token is required.")]
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

  // NEW-3: Forgot Password
  public class ForgotPasswordDto
  {
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string Email { get; set; } = string.Empty;
  }

  // NEW-3: Reset Password
  public class ResetPasswordDto
  {
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Token is required.")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "New Password is required.")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm Password is required.")]
    [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
  }
}