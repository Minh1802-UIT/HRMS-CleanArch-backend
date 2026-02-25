using System.Collections.Generic;

namespace Employee.Application.Common.Interfaces
{
  public interface ITokenService
  {
    string GenerateJwtToken(string userId, string email, string fullName, IList<string> roles, string? employeeId = null);
    string GenerateRefreshToken();
    System.Security.Claims.ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
  }
}