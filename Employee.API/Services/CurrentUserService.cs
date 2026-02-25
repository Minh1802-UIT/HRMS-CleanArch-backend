using System.Security.Claims;
using Employee.Application.Common.Interfaces;

namespace Employee.API.Services
{
  public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUser
  {

    // Lấy UserID từ Claim "sub" hoặc ClaimTypes.NameIdentifier trong Token
    public string UserId => httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    public string? EmployeeId => httpContextAccessor.HttpContext?.User?.FindFirstValue("EmployeeId");
    public string? UserName => httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name);
    public bool IsInRole(string role)
    {
      return httpContextAccessor.HttpContext?.User?.IsInRole(role) ?? false;
    }
  }
}