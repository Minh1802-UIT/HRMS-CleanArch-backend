using System.ComponentModel.DataAnnotations;

namespace Employee.Application.Features.Auth.Dtos
{
    // 1. VIEW DTO (Output - Giữ nguyên của bạn, chỉ thêm Status)
    public class UserDto
    {
        public string Id { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? EmployeeId { get; set; }
        public IList<string> Roles { get; set; } = new List<string>();

        // 👉 Bổ sung theo BA: Trạng thái hoạt động (Active/Inactive)
        public bool IsActive { get; set; }
        // Flag: user must change their auto-generated password on first login
        public bool MustChangePassword { get; set; }
    }

    // 2. UPDATE ROLES DTO (Dùng cho API: PUT /api/auth/roles/{userId})
    public class UpdateUserRolesDto
    {
        // Không cần UserId ở đây vì đã có trên URL

        [Required(ErrorMessage = "Role list is required.")]
        public List<string> Roles { get; set; } = new List<string>();
    }

    // 3. UPDATE STATUS DTO (Dùng cho API: PUT /api/auth/status/{userId})
    // 👉 Feature này cần thiết cho quy trình "Offboarding" và "Re-hiring" trong BA
    public class UpdateUserStatusDto
    {
        [Required(ErrorMessage = "Status is required.")]
        public bool IsActive { get; set; }
    }

    // 4. CHANGE PASSWORD DTO (User tự đổi mật khẩu)
    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "Current Password is required.")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "New Password is required.")]
        [MinLength(6, ErrorMessage = "New Password must be at least 6 characters long.")]
        // Validate mật khẩu mới không được trùng mật khẩu cũ (Logic này làm ở Service)
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm Password is required.")]
        [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}