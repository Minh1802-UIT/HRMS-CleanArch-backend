using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Employee.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Employee.Infrastructure.Services
{
  public class TokenService : ITokenService
  {
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config)
    {
      _config = config;
    }

    public string GenerateJwtToken(string userId, string email, string fullName, IList<string> roles, string? employeeId = null)
    {
      var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, fullName),
                new Claim(ClaimTypes.Email, email)
            };

      // Include EmployeeId so CurrentUserService can resolve the MongoDB employee document
      // from JWT claims without an extra database lookup.
      if (!string.IsNullOrEmpty(employeeId))
        claims.Add(new Claim("EmployeeId", employeeId));

      foreach (var role in roles)
      {
        claims.Add(new Claim(ClaimTypes.Role, role));
      }

      var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JwtSettings:Key"] ?? throw new InvalidOperationException("JWT Key is missing")));
      var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

      var durationStr = _config["JwtSettings:DurationInMinutes"] ?? "60";
      var expiry = DateTime.UtcNow.AddMinutes(double.Parse(durationStr));

      var token = new JwtSecurityToken(
          issuer: _config["JwtSettings:Issuer"],
          audience: _config["JwtSettings:Audience"],
          claims: claims,
          expires: expiry,
          signingCredentials: creds
      );

      return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
      var randomNumber = new byte[64];
      using var rng = RandomNumberGenerator.Create();
      rng.GetBytes(randomNumber);
      return Convert.ToBase64String(randomNumber);
    }

    /// <summary>
    /// SHA-256 hash of the raw token, encoded as Base64-URL (no padding).
    /// This is what gets stored in the DB; the raw token lives only in the cookie.
    /// </summary>
    public string HashToken(string token)
    {
      var bytes = System.Security.Cryptography.SHA256.HashData(
          System.Text.Encoding.UTF8.GetBytes(token));
      return Convert.ToBase64String(bytes)
                    .Replace('+', '-')
                    .Replace('/', '_')
                    .TrimEnd('=');
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
      var key = Encoding.UTF8.GetBytes(_config["JwtSettings:Key"] ?? throw new InvalidOperationException("JWT Key is missing"));

      var tokenValidationParameters = new TokenValidationParameters
      {
        ValidateAudience = true,
        ValidateIssuer = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidIssuer = _config["JwtSettings:Issuer"],
        ValidAudience = _config["JwtSettings:Audience"],
        ValidateLifetime = false
      };

      var tokenHandler = new JwtSecurityTokenHandler();
      var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

      if (securityToken is not JwtSecurityToken jwtSecurityToken ||
          !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
      {
        return null;
      }

      return principal;
    }
  }
}