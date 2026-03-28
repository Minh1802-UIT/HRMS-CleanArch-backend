using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Employee.Application.Common.Interfaces;
using Employee.Application.Features.Auth.Dtos;
using Moq;
using System;

namespace Employee.IntegrationTests;

/// <summary>
/// Custom WebApplicationFactory for integration testing.
/// Uses Testcontainers (via <see cref="IntegrationTestFixture"/>) to spin up an
/// isolated MongoDB and Redis for the full test session.
///
/// Overrides:
/// - Database connection strings → Testcontainers MongoDB + Redis
/// - Background services → removed (not needed in tests)
/// - Hangfire / background jobs → no-op mock
/// - Rate limiter → set to 1000 req/min (effectively disabled in tests)
/// </summary>
public class EmployeeApiFactory : WebApplicationFactory<Program>
{
  /// <summary>
  /// Injected by xUnit via <see cref="ApiCollection"/> / <see cref="IntegrationTestFixture"/>.
  /// </summary>
  private readonly IntegrationTestFixture _fixture;

  /// <summary>
  /// Exposes the mocked IIdentityService so individual test classes can add extra
  /// <c>.Setup()</c> calls for their specific scenarios.
  /// </summary>
  public Mock<IIdentityService> MockIdentity { get; } = new Mock<IIdentityService>();

  public EmployeeApiFactory(IntegrationTestFixture fixture)
  {
    Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
    _fixture = fixture;
  }

  protected override void ConfigureWebHost(IWebHostBuilder builder)
  {
    builder.UseEnvironment("Testing");

    // ─────────────────────────────────────────────────────────────────
    // 1. Configuration — override app settings with testcontainer values
    // ─────────────────────────────────────────────────────────────────
    builder.UseSetting("JwtSettings:Key", "IntegrationTestSecretKey_AtLeast32Characters_Long!!");
    builder.UseSetting("JwtSettings:Issuer", "EmployeeAPI");
    builder.UseSetting("JwtSettings:Audience", "EmployeeClient");
    builder.UseSetting("JwtSettings:DurationInMinutes", "60");

    // ← Testcontainers-backed connections (no localhost dependency)
    builder.UseSetting("EmployeeDatabaseSettings:ConnectionString", _fixture.MongoConnectionString);
    builder.UseSetting("EmployeeDatabaseSettings:DatabaseName", _fixture.DatabaseName);
    builder.UseSetting("RedisSettings:ConnectionString", _fixture.RedisConnectionString);

    builder.UseSetting("CorsSettings:AllowedOrigins:0", "http://localhost:4200");

    builder.UseSetting("BackgroundJobs:LeaveAccrualIntervalHours", "9999");
    builder.UseSetting("BackgroundJobs:PayrollIntervalHours", "9999");
    builder.UseSetting("BackgroundJobs:ContractExpirationIntervalHours", "9999");

    builder.UseSetting("EmailSettings:SmtpHost", "localhost");
    builder.UseSetting("EmailSettings:SenderEmail", "test@test.com");
    builder.UseSetting("EmailSettings:Password", "test");

    // ─────────────────────────────────────────────────────────────────
    // 2. Service Overrides for Test Environment
    // ─────────────────────────────────────────────────────────────────
    builder.ConfigureServices(services =>
    {
      // Remove background hosted services (they need a running MongoDB/Redis in prod).
      // Testcontainers already provides these, but we don't need the hosted-service wrappers.
      services.RemoveAll<IHostedService>();

      // Replace Hangfire / background job service with a no-op mock.
      var mockJobService = new Mock<IBackgroundJobService>();
      services.Replace(ServiceDescriptor.Singleton(mockJobService.Object));

      // ── Default IIdentityService mock setups ─────────────────────────
      // "invalid credentials" → throw UnauthorizedAccessException
      MockIdentity.Setup(x => x.LoginAsync("nonexistent@test.com", "WrongPassword123"))
                  .ThrowsAsync(new UnauthorizedAccessException("Invalid credentials"));

      // RevokeAllRefreshTokensAsync succeeds silently (used by Logout handler)
      MockIdentity.Setup(x => x.RevokeAllRefreshTokensAsync(It.IsAny<string>()))
                  .Returns(Task.CompletedTask);

      // RefreshTokenAsync with "valid-refresh-token" → return a rotated token
      MockIdentity.Setup(x => x.RefreshTokenAsync(It.IsAny<string>(), "valid-refresh-token"))
                  .ReturnsAsync(new LoginResponseDto
                  {
                    AccessToken = "new-access-token",
                    RefreshToken = "new-refresh-token",
                    TokenType = "Bearer",
                    ExpiresIn = 3600,
                    User = new UserDto { Username = "testuser", Email = "test@test.com" }
                  });

      // RefreshTokenAsync with "revoked-refresh-token" → simulate token reuse detection
      MockIdentity.Setup(x => x.RefreshTokenAsync(It.IsAny<string>(), "revoked-refresh-token"))
                  .ThrowsAsync(new UnauthorizedAccessException("Token reuse detected — all sessions have been revoked."));

      services.Replace(ServiceDescriptor.Scoped(_ => MockIdentity.Object));
    });
  }
}
