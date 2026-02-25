using System.Collections.Generic;

namespace Employee.Application.Common.Interfaces
{
  public interface ITokenService
  {
    string GenerateJwtToken(string userId, string email, string fullName, IList<string> roles, string? employeeId = null);
    string GenerateRefreshToken();
    System.Security.Claims.ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);

    /// <summary>
    /// Returns the SHA-256 / Base64-URL hash of <paramref name="token"/>.
    /// Store this hash in the DB; always compare hashes, never raw tokens.
    /// </summary>
    string HashToken(string token);
  }
}