using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using Employee.Application.Common.Interfaces;
using Employee.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Employee.UnitTests.Infrastructure.Services;

public class TokenServiceTests
{
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly TokenService _tokenService;
    private readonly IConfiguration _configuration;

    public TokenServiceTests()
    {
        _configurationMock = new Mock<IConfiguration>();
        
        var inMemorySettings = new Dictionary<string, string?> {
            { "JwtSettings:Key", "ThisIsAVeryLongSecretKeyForTestingPurposes123456" },
            { "JwtSettings:Issuer", "TestIssuer" },
            { "JwtSettings:Audience", "TestAudience" },
            { "JwtSettings:DurationInMinutes", "60" }
        };
        
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
        
        _tokenService = new TokenService(_configuration);
    }

    [Fact]
    public void GenerateJwtToken_WithValidParameters_ShouldReturnValidToken()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var email = "test@example.com";
        var fullName = "Test User";
        var roles = new List<string> { "Admin", "HR" };
        var employeeId = Guid.NewGuid().ToString();

        // Act
        var token = _tokenService.GenerateJwtToken(userId, email, fullName, roles, employeeId);

        // Assert
        token.Should().NotBeNullOrEmpty();
        
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        
        // Use ToList() to enumerate claims
        var claimsList = jwtToken.Claims.ToList();
        
        claimsList.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == userId);
        claimsList.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == fullName);
        claimsList.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == email);
        claimsList.Should().Contain(c => c.Type == "EmployeeId" && c.Value == employeeId);
        claimsList.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "Admin");
        claimsList.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "HR");
    }

    [Fact]
    public void GenerateJwtToken_WithoutEmployeeId_ShouldNotIncludeEmployeeIdClaim()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var email = "test@example.com";
        var fullName = "Test User";
        var roles = new List<string> { "User" };

        // Act
        var token = _tokenService.GenerateJwtToken(userId, email, fullName, roles);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        
        jwtToken.Claims.Should().NotContain(c => c.Type == "EmployeeId");
    }

    [Fact]
    public void GenerateJwtToken_WithEmptyRoles_ShouldReturnTokenWithoutRoleClaims()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var email = "test@example.com";
        var fullName = "Test User";
        var roles = new List<string>();

        // Act
        var token = _tokenService.GenerateJwtToken(userId, email, fullName, roles);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        
        jwtToken.Claims.Should().NotContain(c => c.Type == ClaimTypes.Role);
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturn64ByteBase64UrlString()
    {
        // Act
        var refreshToken = _tokenService.GenerateRefreshToken();

        // Assert
        refreshToken.Should().NotBeNullOrEmpty();
        refreshToken.Length.Should().BeGreaterThan(40); // Base64Url of 64 bytes
        
        // Should not contain + / or = (Base64Url encoding)
        refreshToken.Should().NotContain("+");
        refreshToken.Should().NotContain("/");
        refreshToken.Should().NotContain("=");
    }

    [Fact]
    public void GenerateRefreshToken_CallTwice_ShouldReturnDifferentTokens()
    {
        // Act
        var token1 = _tokenService.GenerateRefreshToken();
        var token2 = _tokenService.GenerateRefreshToken();

        // Assert
        token1.Should().NotBe(token2);
    }

    [Fact]
    public void HashToken_WithValidToken_ShouldReturnSHA256Hash()
    {
        // Arrange
        var token = "test-token-12345";

        // Act
        var hash = _tokenService.HashToken(token);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Should().NotContain("+");
        hash.Should().NotContain("/");
        hash.Should().NotContain("=");
    }

    [Fact]
    public void HashToken_SameToken_ShouldReturnSameHash()
    {
        // Arrange
        var token = "test-token-12345";

        // Act
        var hash1 = _tokenService.HashToken(token);
        var hash2 = _tokenService.HashToken(token);

        // Assert
        hash1.Should().Be(hash2);
    }

    [Fact]
    public void HashToken_DifferentTokens_ShouldReturnDifferentHashes()
    {
        // Arrange
        var token1 = "test-token-12345";
        var token2 = "test-token-67890";

        // Act
        var hash1 = _tokenService.HashToken(token1);
        var hash2 = _tokenService.HashToken(token2);

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_WithValidToken_ShouldReturnClaimsPrincipal()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var email = "test@example.com";
        var fullName = "Test User";
        var roles = new List<string> { "Admin" };
        
        var token = _tokenService.GenerateJwtToken(userId, email, fullName, roles);

        // Act
        var principal = _tokenService.GetPrincipalFromExpiredToken(token);

        // Assert
        principal.Should().NotBeNull();
        principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value.Should().Be(userId);
        principal?.FindFirst(ClaimTypes.Email)?.Value.Should().Be(email);
        principal?.FindFirst(ClaimTypes.Name)?.Value.Should().Be(fullName);
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_WithInvalidToken_ShouldThrow()
    {
        // Arrange - Use a completely malformed token (not a valid JWT)
        var invalidToken = "not-a-valid-jwt";

        // Act & Assert - Should throw for malformed token
        var act = () => _tokenService.GetPrincipalFromExpiredToken(invalidToken);
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_WithTamperedToken_ShouldThrow()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var email = "test@example.com";
        var fullName = "Test User";
        var roles = new List<string> { "Admin" };
        
        var token = _tokenService.GenerateJwtToken(userId, email, fullName, roles);
        // Tamper with the signature part
        var parts = token.Split('.');
        parts[2] = "tampered_signature";
        var tamperedToken = string.Join(".", parts);

        // Act & Assert - Should throw due to signature mismatch
        var act = () => _tokenService.GetPrincipalFromExpiredToken(tamperedToken);
        act.Should().Throw<Exception>();
    }
}
