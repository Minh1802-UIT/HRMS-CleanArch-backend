using Employee.Application.Common.Models;
using Employee.Application.Features.Auth.Dtos;

namespace Employee.Application.Common.Interfaces
{
  public interface IIdentityService
  {
    Task<string?> GetUserNameAsync(string userId);
    Task<bool> IsInRoleAsync(string userId, string role);
    Task<bool> AuthorizeAsync(string userId, string policyName);
    Task<(Result Result, string UserId)> CreateUserAsync(string userName, string email, string password, string fullName, string? employeeId);
    Task<Result> DeleteUserAsync(string userId);
    Task<Result> DeleteByEmployeeIdAsync(string employeeId);

    // Auth specific
    Task<List<UserDto>> GetUsersAsync();
    Task<PagedResult<UserDto>> GetPagedUsersAsync(PaginationParams parameters);
    Task<UserDto?> GetUserByUsernameAsync(string username);
    Task<UserDto?> GetUserByEmailAsync(string email);
    Task<string> RegisterAsync(RegisterDto dto);
    Task<LoginResponseDto> LoginAsync(string userName, string password);
    Task<Result> AssignRoleAsync(string userId, string roleName);
    Task<LoginResponseDto> RefreshTokenAsync(string accessToken, string refreshToken);
    Task<Result> ChangePasswordAsync(string userId, string currentPassword, string newPassword);
    Task<Result> ResetPasswordAsync(string email, string token, string newPassword);
    Task<string> GenerateForgotPasswordTokenAsync(string email);
    Task<Result> UpdateUserRolesAsync(string userId, List<string> roles);
    Task<Result> ToggleUserStatusAsync(string userId, bool isActive);
    Task<List<string>> GetRolesAsync();
    Task<Result> CreateRoleAsync(string roleName);
  }

  public class Result
  {
    internal Result(bool succeeded, IEnumerable<string> errors)
    {
      Succeeded = succeeded;
      Errors = errors.ToArray();
    }

    public bool Succeeded { get; set; }
    public string[] Errors { get; set; }

    public static Result Success()
    {
      return new Result(true, Array.Empty<string>());
    }

    public static Result Failure(IEnumerable<string> errors)
    {
      return new Result(false, errors);
    }
  }
}
