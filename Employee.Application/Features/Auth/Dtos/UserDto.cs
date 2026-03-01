
namespace Employee.Application.Features.Auth.Dtos
{
    // 1. VIEW DTO (Output - Gi? nguyên c?a b?n, ch? thêm Status)
    public class UserDto
    {
        public string Id { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? EmployeeId { get; set; }
        public IList<string> Roles { get; set; } = new List<string>();

        // ?? B? sung theo BA: Tr?ng thái ho?t d?ng (Active/Inactive)
        public bool IsActive { get; set; }
        // Flag: user must change their auto-generated password on first login
        public bool MustChangePassword { get; set; }
    }

    // 2. UPDATE ROLES DTO (Dùng cho API: PUT /api/auth/roles/{userId})
    public class UpdateUserRolesDto
    {
        // Không c?n UserId ? dây vì dã có trên URL
        public List<string> Roles { get; set; } = new List<string>();
    }

    // 3. UPDATE STATUS DTO (Dùng cho API: PUT /api/auth/status/{userId})
    // ?? Feature này c?n thi?t cho quy trình "Offboarding" và "Re-hiring" trong BA
    public class UpdateUserStatusDto
    {
        public bool IsActive { get; set; }
    }

    // 4. CHANGE PASSWORD DTO (User t? d?i m?t kh?u)
    public class ChangePasswordDto
    {
        public string CurrentPassword { get; set; } = string.Empty;
        // Validate m?t kh?u m?i không du?c trùng m?t kh?u cu (Logic này làm ? Service)
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}