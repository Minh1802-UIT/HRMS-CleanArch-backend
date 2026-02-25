using Employee.Application.Common.Models;
using Employee.API.Common;
using Employee.Domain.Constants;
using Employee.Application.Features.Auth.Dtos;
using Employee.Application.Common.Interfaces;
using Employee.Application.Features.Auth.Commands.Register;
using Employee.Application.Features.Auth.Commands.Login;
using Employee.Application.Features.Auth.Commands.CreateRole;
using Employee.Application.Features.Auth.Commands.AssignRole;
using Employee.Application.Features.Auth.Commands.UpdateUserRoles;
using Employee.Application.Features.Auth.Commands.ToggleUserStatus;
using Employee.Application.Features.Auth.Commands.ChangePassword;
using Employee.Application.Features.Auth.Commands.RefreshToken;
using Employee.Application.Features.Auth.Commands.ForgotPassword;
using Employee.Application.Features.Auth.Commands.ResetPassword;
using Employee.Application.Features.Auth.Queries.GetUsers;
using Employee.Application.Features.Auth.Queries.GetRoles;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Employee.API.Endpoints.Auth
{
  public static class AuthHandlers
  {
    // 1. REGISTER (Admin Only)
    public static async Task<IResult> Register([FromBody] RegisterDto dto, ISender sender)
    {
      await sender.Send(new RegisterCommand
      {
        Username = dto.Username,
        Email = dto.Email,
        Password = dto.Password,
        FullName = dto.FullName,
        EmployeeId = dto.EmployeeId
      });
      return ResultUtils.Created("Account registered successfully.");
    }

    // 2. LOGIN (Public) — sets refresh token as httpOnly cookie
    public static async Task<IResult> Login([FromBody] LoginDto dto, ISender sender, HttpContext context)
    {
      var result = await sender.Send(new LoginCommand
      {
        Username = dto.Username,
        Password = dto.Password
      });

      // Secure refresh token in httpOnly cookie — never exposed to JavaScript
      context.Response.Cookies.Append("refreshToken", result.RefreshToken, new CookieOptions
      {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Expires = DateTimeOffset.UtcNow.AddDays(7),
        Path = "/"
      });

      // Return access token + user info (no refresh token in body)
      return ResultUtils.Success(new
      {
        accessToken = result.AccessToken,
        tokenType = result.TokenType,
        expiresIn = result.ExpiresIn,
        user = result.User
      }, "Login successfully.");
    }

    // 3. CREATE ROLE (Admin Only)
    public static async Task<IResult> CreateRole([FromBody] CreateRoleDto dto, ISender sender)
    {
      await sender.Send(new CreateRoleCommand { RoleName = dto.RoleName });
      return ResultUtils.Created($"Role '{dto.RoleName}' created successfully.");
    }

    // 4. ASSIGN ROLE (Admin Only)
    public static async Task<IResult> AssignRole([FromBody] AssignRoleDto dto, ISender sender)
    {
      await sender.Send(new AssignRoleCommand
      {
        Username = dto.Username,
        RoleName = dto.RoleName
      });
      return ResultUtils.Success($"Role '{dto.RoleName}' assigned to user '{dto.Username}' successfully.");
    }

    // 5. GET ALL USERS (Admin, HR)
    public static async Task<IResult> GetAllUsers([AsParameters] PaginationParams pagination, ISender sender)
    {
      var users = await sender.Send(new GetUsersQuery { Pagination = pagination });
      return ResultUtils.Success(users, "Retrieved user list successfully.");
    }

    // 6. UPDATE USER ROLES
    public static async Task<IResult> UpdateUserRoles(string userId, [FromBody] UpdateUserRolesDto dto, ISender sender)
    {
      await sender.Send(new UpdateUserRolesCommand
      {
        UserId = userId,
        RoleNames = dto.Roles
      });
      return ResultUtils.Success("User roles updated successfully.");
    }

    public static async Task<IResult> UpdateStatus(string userId, [FromBody] UpdateUserStatusDto dto, ISender sender)
    {
      await sender.Send(new ToggleUserStatusCommand
      {
        UserId = userId,
        IsActive = dto.IsActive
      });
      var statusMsg = dto.IsActive ? "activated" : "deactivated";
      return ResultUtils.Success($"User account has been {statusMsg}.");
    }

    public static async Task<IResult> ChangePassword([FromBody] ChangePasswordDto dto, ISender sender, ICurrentUser currentUser)
    {
      await sender.Send(new ChangePasswordCommand
      {
        UserId = currentUser.UserId,
        CurrentPassword = dto.CurrentPassword,
        NewPassword = dto.NewPassword
      });
      return ResultUtils.Success("Password changed successfully.");
    }

    // 7. GET ALL ROLES (Admin Only)
    public static async Task<IResult> GetRoles(ISender sender)
    {
      var roles = await sender.Send(new GetRolesQuery());
      return ResultUtils.Success(roles, "Retrieved all roles successfully.");
    }

    // 8. REFRESH TOKEN (Public) — reads refresh token from httpOnly cookie
    public static async Task<IResult> RefreshToken([FromBody] RefreshAccessTokenDto dto, ISender sender, HttpContext context)
    {
      // Read refresh token from httpOnly cookie
      var refreshToken = context.Request.Cookies["refreshToken"];
      if (string.IsNullOrEmpty(refreshToken))
        return ResultUtils.Fail("REFRESH_TOKEN_REQUIRED", "Refresh token cookie is missing or expired. Please log in again.", 401);

      var result = await sender.Send(new RefreshTokenCommand
      {
        AccessToken = dto.AccessToken,
        RefreshToken = refreshToken
      });

      // Rotate refresh token cookie
      context.Response.Cookies.Append("refreshToken", result.RefreshToken, new CookieOptions
      {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Expires = DateTimeOffset.UtcNow.AddDays(7),
        Path = "/"
      });

      return ResultUtils.Success(new
      {
        accessToken = result.AccessToken,
        tokenType = result.TokenType,
        expiresIn = result.ExpiresIn
      }, "Token refreshed successfully.");
    }

    // 11. LOGOUT (Public) — clears the httpOnly refresh token cookie
    public static IResult Logout(HttpContext context)
    {
      context.Response.Cookies.Delete("refreshToken", new CookieOptions
      {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = "/"
      });
      return ResultUtils.Success("Logged out successfully.");
    }

    // 9. FORGOT PASSWORD (Public)
    public static async Task<IResult> ForgotPassword([FromBody] ForgotPasswordDto dto, ISender sender)
    {
      // C4-FIX: Token is sent via EmailService, NOT returned in API response
      await sender.Send(new ForgotPasswordCommand { Email = dto.Email });
      return ResultUtils.Success("If the email exists, a reset link has been sent.");
    }

    // 10. RESET PASSWORD (Public)
    public static async Task<IResult> ResetPassword([FromBody] ResetPasswordDto dto, ISender sender)
    {
      await sender.Send(new ResetPasswordCommand
      {
        Email = dto.Email,
        Token = dto.Token,
        NewPassword = dto.NewPassword
      });
      return ResultUtils.Success("Password reset successfully.");
    }
  }
}
