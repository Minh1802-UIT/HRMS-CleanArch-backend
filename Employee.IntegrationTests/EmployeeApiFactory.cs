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
/// Overrides configuration to use a test database,
/// disables background services for clean tests.
/// Rate limiter is set to very high values via config.
/// </summary>
public class EmployeeApiFactory : WebApplicationFactory<Program>
{
  protected override void ConfigureWebHost(IWebHostBuilder builder)
  {
    builder.UseEnvironment("Testing");

    // ─────────────────────────────────────────────
    // 1. Configuration — injected BEFORE Program.cs reads it
    // ─────────────────────────────────────────────
    builder.UseSetting("JwtSettings:Key", "IntegrationTestSecretKey_AtLeast32Characters_Long!!");
    builder.UseSetting("JwtSettings:Issuer", "EmployeeAPI");
    builder.UseSetting("JwtSettings:Audience", "EmployeeClient");
    builder.UseSetting("JwtSettings:DurationInMinutes", "60");

    builder.UseSetting("EmployeeDatabaseSettings:ConnectionString", "mongodb://localhost:27017");
    builder.UseSetting("EmployeeDatabaseSettings:DatabaseName", "EmployeeCleanDB_IntegrationTest");

    builder.UseSetting("RedisSettings:ConnectionString", "localhost:6379");
    builder.UseSetting("CorsSettings:AllowedOrigins:0", "http://localhost:4200");

    builder.UseSetting("BackgroundJobs:LeaveAccrualIntervalHours", "9999");
    builder.UseSetting("BackgroundJobs:PayrollIntervalHours", "9999");
    builder.UseSetting("BackgroundJobs:ContractExpirationIntervalHours", "9999");

    builder.UseSetting("EmailSettings:SmtpHost", "localhost");
    builder.UseSetting("EmailSettings:SenderEmail", "test@test.com");
    builder.UseSetting("EmailSettings:Password", "test");

    // ─────────────────────────────────────────────
    // 2. Service Overrides for Test Environment
    // ─────────────────────────────────────────────
    builder.ConfigureServices(services =>
    {
      // Remove background services (they need real MongoDB)
      services.RemoveAll<IHostedService>();

      // Mock Identity Service to avoid MongoDB dependency in Auth Tests
      var mockIdentity = new Moq.Mock<IIdentityService>();

      // Setup default behavior for "invalid credentials" test
      mockIdentity.Setup(x => x.LoginAsync("nonexistent@test.com", "WrongPassword123"))
                  .ThrowsAsync(new UnauthorizedAccessException("Invalid credentials"));

      services.Replace(ServiceDescriptor.Scoped(_ => mockIdentity.Object));
    });
  }
}
