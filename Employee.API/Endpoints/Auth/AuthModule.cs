using Carter;
using Employee.API.Common;
using Employee.Application.Features.Auth.Dtos;

namespace Employee.API.Endpoints.Auth
{
  public class AuthModule : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      // N1-FIX: Apply rate limiter to auth endpoints
      var group = app.MapGroup("/api/auth")
                     .WithTags("Authentication")
                     .RequireRateLimiting("auth");

      // ---------------------------
      // 1. PUBLIC ROUTES (Ai cũng dùng được)
      // ---------------------------

      group.MapPost("/login", AuthHandlers.Login)
           .AddEndpointFilter<ValidationFilter<LoginDto>>() // 👈 Validate LoginDto
           .AllowAnonymous(); // Mở công khai

      // ---------------------------
      // 2. PROTECTED ROUTES (Cần đăng nhập)
      // ---------------------------

      // 2.1 Register (Chỉ Admin tạo user mới)
      group.MapPost("/register", AuthHandlers.Register)
           .AddEndpointFilter<ValidationFilter<RegisterDto>>() // 👈 Validate RegisterDto
           .RequireAuthorization(p => p.RequireRole("Admin"));

      // 2.2 Quản lý Role (Chỉ Admin)
      group.MapPost("/role", AuthHandlers.CreateRole)
           .AddEndpointFilter<ValidationFilter<CreateRoleDto>>()
           .RequireAuthorization(p => p.RequireRole("Admin"));

      group.MapPost("/assign-role", AuthHandlers.AssignRole)
           .AddEndpointFilter<ValidationFilter<AssignRoleDto>>()
           .RequireAuthorization(p => p.RequireRole("Admin"));

      // 2.3 Xem danh sách (Admin hoặc HR)
      group.MapGet("/users", AuthHandlers.GetAllUsers)
           .RequireAuthorization(p => p.RequireRole("Admin", "HR"));

      // 2.4 Cập nhật Role cho User cụ thể
      // ⚠️ Lưu ý: Thay đổi quyền hạn là hành động nguy hiểm, nên để Admin only
      group.MapPut("/roles/{userId}", AuthHandlers.UpdateUserRoles)
           // .AddEndpointFilter<ValidationFilter<UpdateUserRolesDto>>() // Nếu bạn có Validation cho DTO này
           .RequireAuthorization(p => p.RequireRole("Admin"));

      // 1. Khóa/Mở khóa User (Admin/HR Only)
      group.MapPut("/status/{userId}", AuthHandlers.UpdateStatus)
           .AddEndpointFilter<ValidationFilter<UpdateUserStatusDto>>()
           .RequireAuthorization(p => p.RequireRole("Admin", "HR"));

      group.MapPost("/change-password", AuthHandlers.ChangePassword)
           .AddEndpointFilter<ValidationFilter<ChangePasswordDto>>()
           .RequireAuthorization();

      // 2.5 Get All Roles (Admin Only)
      group.MapGet("/roles", AuthHandlers.GetRoles)
           .RequireAuthorization(p => p.RequireRole("Admin"));

               // ---------------------------
               // 3. NEW-2: REFRESH TOKEN (Public) — uses httpOnly cookie for refresh token
               // ---------------------------
               group.MapPost("/refresh-token", AuthHandlers.RefreshToken)
           .AddEndpointFilter<ValidationFilter<RefreshAccessTokenDto>>()
           .AllowAnonymous();

               // LOGOUT — clears the httpOnly refresh token cookie
               group.MapPost("/logout", AuthHandlers.Logout)
                    .AllowAnonymous();

      // ---------------------------
      // 4. NEW-3: FORGOT / RESET PASSWORD (Public)
      // ---------------------------
      group.MapPost("/forgot-password", AuthHandlers.ForgotPassword)
           .AddEndpointFilter<ValidationFilter<ForgotPasswordDto>>()
           .AllowAnonymous();

      group.MapPost("/reset-password", AuthHandlers.ResetPassword)
           .AddEndpointFilter<ValidationFilter<ResetPasswordDto>>()
           .AllowAnonymous();
    }
  }
}