using Employee.Application.Common.Interfaces;
using Employee.Application.Common.Exceptions;
using Employee.Application.Common.Models;
using Employee.Application.Features.Auth.Dtos;
using Employee.Infrastructure.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Employee.Infrastructure.Identity
{
  public class IdentityService : IIdentityService
  {
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _config;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ITokenService tokenService,
        IConfiguration config)
    {
      _userManager = userManager;
      _roleManager = roleManager;
      _tokenService = tokenService;
      _config = config;
    }

    public async Task<string?> GetUserNameAsync(string userId)
    {
      var user = await _userManager.FindByIdAsync(userId);
      return user?.UserName;
    }

    public async Task<bool> IsInRoleAsync(string userId, string role)
    {
      var user = await _userManager.FindByIdAsync(userId);
      return user != null && await _userManager.IsInRoleAsync(user, role);
    }

    public async Task<bool> AuthorizeAsync(string userId, string policyName)
    {
      // Treat policyName as a role name — covers role-based access control scenarios.
      // For complex policies (resource-based, claim-based) register a proper
      // IAuthorizationService with a policy provider instead.
      return await IsInRoleAsync(userId, policyName);
    }

    public async Task<(Result Result, string UserId)> CreateUserAsync(string userName, string email, string password, string fullName, string? employeeId)
    {
      var user = new ApplicationUser
      {
        UserName = userName,
        Email = email,
        FullName = fullName,
        EmployeeId = employeeId,
        IsActive = true
      };

      var result = await _userManager.CreateAsync(user, password);

      return (result.ToApplicationResult(), user.Id.ToString());
    }

    public async Task<Result> DeleteUserAsync(string userId)
    {
      var user = await _userManager.FindByIdAsync(userId);

      if (user != null)
      {
        var result = await _userManager.DeleteAsync(user);
        return result.ToApplicationResult();
      }

      return Result.Success();
    }

    public async Task<Result> DeleteByEmployeeIdAsync(string employeeId)
    {
      var users = _userManager.Users.Where(u => u.EmployeeId == employeeId).ToList();
      if (!users.Any()) return Result.Success();

      var errors = new List<string>();
      foreach (var user in users)
      {
        var result = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
        if (!result.Succeeded)
        {
          errors.AddRange(result.Errors.Select(e => e.Description));
        }
      }

      return errors.Any() ? Result.Failure(errors) : Result.Success();
    }

    public async Task<List<UserDto>> GetUsersAsync()
    {
      var users = _userManager.Users.ToList();
      var userDtos = new List<UserDto>();

      foreach (var user in users)
      {
        var roles = await _userManager.GetRolesAsync(user);
        userDtos.Add(MapToUserDto(user, roles));
      }

      return userDtos;
    }

    public async Task<PagedResult<UserDto>> GetPagedUsersAsync(PaginationParams parameters)
    {
      var pageNumber = parameters.PageNumber ?? 1;
      var pageSize = parameters.PageSize ?? 20;

      var query = _userManager.Users;

      if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
      {
        var searchTerm = parameters.SearchTerm;
        query = query.Where(u =>
            (u.UserName != null && u.UserName.Contains(searchTerm)) ||
            (u.Email != null && u.Email.Contains(searchTerm)) ||
            (u.FullName != null && u.FullName.Contains(searchTerm)));
      }

      var totalCount = query.Count();
      var users = query
          .Skip((pageNumber - 1) * pageSize)
          .Take(pageSize)
          .ToList();

      var userDtos = new List<UserDto>();
      foreach (var user in users)
      {
        var roles = await _userManager.GetRolesAsync(user);
        userDtos.Add(MapToUserDto(user, roles));
      }

      return new PagedResult<UserDto>
      {
        Items = userDtos,
        TotalCount = totalCount,
        PageNumber = pageNumber,
        PageSize = pageSize
      };
    }

    public async Task<UserDto?> GetUserByUsernameAsync(string username)
    {
      var user = await _userManager.FindByNameAsync(username);
      if (user == null) return null;

      var roles = await _userManager.GetRolesAsync(user);
      return MapToUserDto(user, roles);
    }

    public async Task<UserDto?> GetUserByEmailAsync(string email)
    {
      var user = await _userManager.FindByEmailAsync(email);
      if (user == null) return null;

      var roles = await _userManager.GetRolesAsync(user);
      return MapToUserDto(user, roles);
    }

    public async Task<Result> AssignRoleAsync(string userId, string roleName)
    {
      var user = await _userManager.FindByIdAsync(userId);
      if (user == null) return Result.Failure(new[] { "User not found." });

      if (!await _roleManager.RoleExistsAsync(roleName))
      {
        await _roleManager.CreateAsync(new ApplicationRole(roleName));
      }

      var result = await _userManager.AddToRoleAsync(user, roleName);
      return result.ToApplicationResult();
    }

    public async Task<string> RegisterAsync(RegisterDto dto)
    {
      var existingUser = await _userManager.FindByNameAsync(dto.Username);
      if (existingUser != null) throw new ConflictException("Username đã tồn tại");

      if (!string.IsNullOrEmpty(dto.Email))
      {
        var existingEmail = await _userManager.FindByEmailAsync(dto.Email);
        if (existingEmail != null) throw new ConflictException("Email đã tồn tại");
      }

      var user = new ApplicationUser
      {
        UserName = dto.Username,
        Email = dto.Email,
        FullName = dto.FullName,
        EmployeeId = dto.EmployeeId,
        EmailConfirmed = true,
        LockoutEnabled = true,
        IsActive = true,
        MustChangePassword = dto.MustChangePassword
      };

      var result = await _userManager.CreateAsync(user, dto.Password);
      if (!result.Succeeded)
      {
        throw new ValidationException(string.Join(", ", result.Errors.Select(e => e.Description)));
      }

      if (!await _roleManager.RoleExistsAsync("Employee"))
      {
        await _roleManager.CreateAsync(new ApplicationRole("Employee"));
      }

      await _userManager.AddToRoleAsync(user, "Employee");

      return user.Id.ToString();
    }

    public async Task<LoginResponseDto> LoginAsync(string userName, string password)
    {
      var user = await _userManager.FindByNameAsync(userName.Trim())
                 ?? await _userManager.FindByEmailAsync(userName.Trim());

      if (user == null)
      {
        throw new UnauthorizedAccessException("Tài khoản không tồn tại.");
      }

      if (user.LockoutEnd != null && user.LockoutEnd > DateTimeOffset.UtcNow)
      {
        throw new UnauthorizedAccessException("Tài khoản đã bị khóa. Liên hệ Admin.");
      }

      // H-4: Explicitly check the IsActive flag in addition to the lockout check.
      // A user can have IsActive=false without a LockoutEnd date (e.g. set directly
      // in the DB or via ToggleUserStatusAsync without using lockout).
      if (!user.IsActive)
      {
        throw new UnauthorizedAccessException("Tài khoản đã bị vô hiệu hóa. Liên hệ Admin.");
      }

      if (!await _userManager.CheckPasswordAsync(user, password))
      {
        throw new UnauthorizedAccessException("Mật khẩu không đúng.");
      }

      var roles = await _userManager.GetRolesAsync(user);
      var token = _tokenService.GenerateJwtToken(user.Id.ToString(), user.Email ?? "", user.FullName, roles, user.EmployeeId);

      var rawRefreshToken = _tokenService.GenerateRefreshToken();
      var familyId = Guid.NewGuid().ToString();

      // Revoke all previous sessions on new login (single-session-per-login policy).
      user.RefreshTokens.ForEach(t => t.IsRevoked = true);
      PruneRefreshTokens(user);

      user.RefreshTokens.Add(new Employee.Infrastructure.Identity.Models.RefreshTokenEntry
      {
        TokenHash = _tokenService.HashToken(rawRefreshToken),
        FamilyId = familyId,
        IssuedAt = DateTime.UtcNow,
        ExpiresAt = DateTime.UtcNow.AddDays(7),
        IsRevoked = false
      });
      await _userManager.UpdateAsync(user);

      return new LoginResponseDto
      {
        AccessToken = token,
        RefreshToken = rawRefreshToken,
        ExpiresIn = int.Parse(_config["JwtSettings:DurationInMinutes"] ?? "60") * 60,
        User = MapToUserDto(user, roles)
      };
    }

    public async Task<LoginResponseDto> RefreshTokenAsync(string accessToken, string rawRefreshToken)
    {
      // ── 1. Validate the access token (expired is fine — we re-issue it) ─────
      var principal = _tokenService.GetPrincipalFromExpiredToken(accessToken);
      if (principal == null)
        throw new UnauthorizedAccessException("Access token không hợp lệ.");

      var username = principal.Identity?.Name;
      if (string.IsNullOrEmpty(username))
        throw new UnauthorizedAccessException("Không tìm thấy thông tin user từ token.");

      var user = await _userManager.FindByNameAsync(username)
                 ?? throw new NotFoundException("User không tồn tại.");

      // ── 2. Hash the incoming token and look it up ─────────────────────────
      var incomingHash = _tokenService.HashToken(rawRefreshToken);
      var entry = user.RefreshTokens.FirstOrDefault(t => t.TokenHash == incomingHash);

      if (entry == null)
      {
        // Completely unknown token — fabricated or already pruned.
        throw new UnauthorizedAccessException("Refresh token không hợp lệ.");
      }

      if (entry.IsRevoked)
      {
        // ── REUSE DETECTION ─────────────────────────────────────────────────
        // An already-used token was re-presented → possible theft.
        // Revoke the ENTIRE family so every rotation from this session is killed.
        foreach (var t in user.RefreshTokens.Where(t => t.FamilyId == entry.FamilyId))
          t.IsRevoked = true;
        await _userManager.UpdateAsync(user);
        throw new UnauthorizedAccessException(
            "Refresh token đã bị thu hồi. Phiên đăng nhập bị chấm dứt vì lý do bảo mật. Vui lòng đăng nhập lại.");
      }

      if (entry.ExpiresAt < DateTime.UtcNow)
        throw new UnauthorizedAccessException("Refresh token đã hết hạn. Vui lòng đăng nhập lại.");

      // ── 3. Rotate: mark current entry used, issue a new sibling ──────────
      entry.IsRevoked = true;

      var newRawToken = _tokenService.GenerateRefreshToken();
      user.RefreshTokens.Add(new Employee.Infrastructure.Identity.Models.RefreshTokenEntry
      {
        TokenHash = _tokenService.HashToken(newRawToken),
        FamilyId = entry.FamilyId,   // same family keeps the reuse-detection chain intact
        IssuedAt = DateTime.UtcNow,
        ExpiresAt = DateTime.UtcNow.AddDays(7),
        IsRevoked = false
      });

      PruneRefreshTokens(user);

      var roles = await _userManager.GetRolesAsync(user);
      var newAccessToken = _tokenService.GenerateJwtToken(
          user.Id.ToString(), user.Email ?? "", user.FullName, roles, user.EmployeeId);

      await _userManager.UpdateAsync(user);

      return new LoginResponseDto
      {
        AccessToken = newAccessToken,
        RefreshToken = newRawToken,
        ExpiresIn = int.Parse(_config["JwtSettings:DurationInMinutes"] ?? "60") * 60,
        User = MapToUserDto(user, roles)
      };
    }

    public async Task RevokeAllRefreshTokensAsync(string userId)
    {
      var user = await _userManager.FindByIdAsync(userId);
      if (user == null) return;

      user.RefreshTokens.ForEach(t => t.IsRevoked = true);
      PruneRefreshTokens(user);
      await _userManager.UpdateAsync(user);
    }

    // Keep the embedded token list bounded.
    // Drop entries revoked > 24 h ago and expired entries > 30 d old.
    private static void PruneRefreshTokens(
        Employee.Infrastructure.Identity.Models.ApplicationUser user)
    {
      var cutoffRevoked = DateTime.UtcNow.AddHours(-24);
      var cutoffExpired = DateTime.UtcNow.AddDays(-30);
      user.RefreshTokens.RemoveAll(t =>
          (t.IsRevoked && t.IssuedAt < cutoffRevoked) ||
          (t.ExpiresAt < cutoffExpired));
    }

    public async Task<Result> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
    {
      var user = await _userManager.FindByIdAsync(userId);
      if (user == null) return Result.Failure(new[] { "User not found." });

      var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
      if (result.Succeeded && user.MustChangePassword)
      {
        // Clear the forced-change-password flag once the user has set their own password.
        user.MustChangePassword = false;
        await _userManager.UpdateAsync(user);
      }
      return result.ToApplicationResult();
    }

    public async Task<Result> ResetPasswordAsync(string email, string token, string newPassword)
    {
      var user = await _userManager.FindByEmailAsync(email);
      if (user == null) return Result.Failure(new[] { "User not found." });

      var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
      return result.ToApplicationResult();
    }

    public async Task<string> GenerateForgotPasswordTokenAsync(string email)
    {
      var user = await _userManager.FindByEmailAsync(email);
      if (user == null) throw new NotFoundException("User not found.");

      return await _userManager.GeneratePasswordResetTokenAsync(user);
    }

    public async Task<Result> UpdateUserRolesAsync(string userId, List<string> roles)
    {
      var user = await _userManager.FindByIdAsync(userId);
      if (user == null) return Result.Failure(new[] { "User not found." });

      var currentRoles = await _userManager.GetRolesAsync(user);
      await _userManager.RemoveFromRolesAsync(user, currentRoles);
      var result = await _userManager.AddToRolesAsync(user, roles);

      return result.ToApplicationResult();
    }

    public async Task<Result> ToggleUserStatusAsync(string userId, bool isActive)
    {
      var user = await _userManager.FindByIdAsync(userId);
      if (user == null) return Result.Failure(new[] { "User not found." });

      user.IsActive = isActive;
      if (!isActive)
      {
        user.LockoutEnd = DateTimeOffset.MaxValue;
      }
      else
      {
        user.LockoutEnd = null;
      }

      var result = await _userManager.UpdateAsync(user);
      return result.ToApplicationResult();
    }

    public async Task<List<string>> GetRolesAsync()
    {
      var roles = _roleManager.Roles.ToList();
      var roleNames = roles.Select(r => r.Name ?? string.Empty).Where(n => !string.IsNullOrEmpty(n)).ToList();
      return await Task.FromResult(roleNames);
    }

    public async Task<Result> CreateRoleAsync(string roleName)
    {
      if (await _roleManager.RoleExistsAsync(roleName))
      {
        return Result.Failure(new[] { "Role already exists." });
      }

      var result = await _roleManager.CreateAsync(new ApplicationRole(roleName));
      return result.ToApplicationResult();
    }

    private UserDto MapToUserDto(ApplicationUser user, IList<string> roles)
    {
      return new UserDto
      {
        Id = user.Id.ToString(),
        Username = user.UserName ?? string.Empty,
        Email = user.Email ?? string.Empty,
        FullName = user.FullName,
        EmployeeId = user.EmployeeId,
        Roles = roles,
        IsActive = user.IsActive,
        MustChangePassword = user.MustChangePassword
      };
    }
  }

  public static class IdentityResultExtensions
  {
    public static Result ToApplicationResult(this IdentityResult result)
    {
      return result.Succeeded
          ? Result.Success()
          : Result.Failure(result.Errors.Select(e => e.Description));
    }
  }
}
