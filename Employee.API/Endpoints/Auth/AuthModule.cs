using Carter;
using Employee.API.Common;
using Employee.Application.Features.Auth.Dtos;

namespace Employee.API.Endpoints.Auth
{
  public class AuthModule : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      var group = app.MapGroup("/api/auth")
                     .WithTags("Authentication")
                     .RequireRateLimiting("auth");

      // ---------------------------
      // 1. PUBLIC ROUTES
      // ---------------------------

      group.MapPost("/login", AuthHandlers.Login)
           .AddEndpointFilter<ValidationFilter<LoginDto>>()
           .AllowAnonymous();

      // ---------------------------
      // 2. PROTECTED ROUTES (Cần đăng nhập)
      // ---------------------------

      // 2.1 Register (Admin only — creates new user accounts)
      group.MapPost("/register", AuthHandlers.Register)
           .AddEndpointFilter<ValidationFilter<RegisterDto>>()
           .RequireAuthorization(p => p.RequireRole("Admin"));

      // 2.2 Manage Roles (Admin only)
      group.MapPost("/role", AuthHandlers.CreateRole)
           .AddEndpointFilter<ValidationFilter<CreateRoleDto>>()
           .RequireAuthorization(p => p.RequireRole("Admin"));

      group.MapPost("/assign-role", AuthHandlers.AssignRole)
           .AddEndpointFilter<ValidationFilter<AssignRoleDto>>()
           .RequireAuthorization(p => p.RequireRole("Admin"));

      // 2.3 List users (Admin or HR)
      group.MapGet("/users", AuthHandlers.GetAllUsers)
           .RequireAuthorization(p => p.RequireRole("Admin", "HR"));

               // 2.4 Update roles for a specific user (Admin only — role changes are destructive)
               group.MapPatch("/roles/{userId}", AuthHandlers.UpdateUserRoles)
                    .RequireAuthorization(p => p.RequireRole("Admin"));

               // 2.5 Activate / deactivate user (Admin or HR)
               group.MapPost("/status/{userId}", AuthHandlers.UpdateStatus)
                    .AddEndpointFilter<ValidationFilter<UpdateUserStatusDto>>()
           .RequireAuthorization(p => p.RequireRole("Admin", "HR"));

      group.MapPost("/change-password", AuthHandlers.ChangePassword)
           .AddEndpointFilter<ValidationFilter<ChangePasswordDto>>()
           .RequireAuthorization();

      // 2.5 Get all roles (Admin only)
      group.MapGet("/roles", AuthHandlers.GetRoles)
           .RequireAuthorization(p => p.RequireRole("Admin"));

      // ---------------------------
      // 3. REFRESH TOKEN (Public) — uses httpOnly cookie
      // Override the group-level "auth" limiter with the more generous "refresh" policy
      // because silent refresh fires automatically on every page load.
      // ---------------------------
      group.MapPost("/refresh-token", AuthHandlers.RefreshToken)
           .AddEndpointFilter<ValidationFilter<RefreshAccessTokenDto>>()
           .AllowAnonymous()
           .RequireRateLimiting("refresh");

      // LOGOUT — clears the httpOnly refresh token cookie; share the "refresh" policy
      // to avoid double-429 on rapid forced-logout flows.
      group.MapPost("/logout", AuthHandlers.Logout)
           .AllowAnonymous()
           .RequireRateLimiting("refresh");

      // ---------------------------
      // 4. FORGOT / RESET PASSWORD (Public)
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