using AspNetCore.Identity.MongoDbCore.Extensions;
using AspNetCore.Identity.MongoDbCore.Infrastructure;
using Carter;
using Employee.API.Middlewares;
using Employee.API.Services;
using Employee.Application;
using Employee.Application.Common.Interfaces;
using Employee.Infrastructure.Identity.Models;
using Employee.Infrastructure;
using Employee.Infrastructure.BackgroundServices;
using Employee.Infrastructure.data.Configurations;
using Employee.Infrastructure.data.Seeding;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.IO.Compression;
using Microsoft.OpenApi.Models;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;

using System.Security.Claims;
using Serilog;

MongoClassMapConfig.Configure();

var builder = WebApplication.CreateBuilder(args);

// NEW: Configure Serilog
builder.Host.UseSerilog((context, parser, configuration) =>
{
  configuration.ReadFrom.Configuration(context.Configuration);
});

// =========================================================================
// 1. CONFIGURATION & SERVICES
// =========================================================================

// 1.1. Minimal API (Carter) & Endpoint Explorer
builder.Services.AddCarter();
builder.Services.AddEndpointsApiExplorer();

// Enforce camelCase JSON globally for ALL responses (Results.Ok, Results.Json, WriteAsJsonAsync)
builder.Services.ConfigureHttpJsonOptions(options =>
{
  options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
  options.SerializerOptions.DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

// NEW: API Versioning
builder.Services.AddApiVersioning(options =>
{
  options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1);
  options.ReportApiVersions = true;
  options.AssumeDefaultVersionWhenUnspecified = true;
  options.ApiVersionReader = Asp.Versioning.ApiVersionReader.Combine(
      new Asp.Versioning.UrlSegmentApiVersionReader(),
      new Asp.Versioning.HeaderApiVersionReader("X-Api-Version"));
})
.AddApiExplorer(options =>
{
  options.GroupNameFormat = "'v'V";
  options.SubstituteApiVersionInUrl = true;
});

// 1.2. MongoDb Identity Configuration
var mongoDbSettings = builder.Configuration.GetSection("EmployeeDatabaseSettings").Get<MongoDbSettings>();
var jwtSettings = builder.Configuration.GetSection("JwtSettings");

// C1-FIX: Validate JWT Key at startup — fail fast if using placeholder
var jwtKey = jwtSettings["Key"];
if (string.IsNullOrEmpty(jwtKey) || jwtKey.Contains("PLACEHOLDER"))
{
  throw new InvalidOperationException(
    "JWT Key is not configured! Set JwtSettings:Key via environment variable or User Secrets. " +
    "Example: dotnet user-secrets set 'JwtSettings:Key' 'YourSecureKeyHere_AtLeast32Characters'");
}

var mongoConfig = new MongoDbIdentityConfiguration
{
  MongoDbSettings = new MongoDbSettings
  {
    ConnectionString = mongoDbSettings?.ConnectionString ?? "mongodb://localhost:27017",
    DatabaseName = mongoDbSettings?.DatabaseName ?? "EmployeeCleanDB"
  },
  IdentityOptionsAction = options =>
  {
    // C3-FIX: Strengthened password policy for HR system
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
  }
};

// 1.3. Clean Architecture Layers
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddEmailService(builder.Configuration, builder.Environment.IsDevelopment());

// 1.4. Core Services (Http, User, Exception)
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUserService>();
builder.Services.AddScoped<Employee.Application.Common.Interfaces.ICorrelationIdProvider, Employee.API.Services.CorrelationIdProvider>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.ConfigureMongoDbIdentity<ApplicationUser, ApplicationRole, Guid>(mongoConfig);

// 1.6. Authentication & JWT
builder.Services.AddAuthentication(options =>
{
  options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
  options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
  options.TokenValidationParameters = new TokenValidationParameters
  {
    ValidateIssuer = true,
    ValidateAudience = true,
    ValidateLifetime = true,
    ValidateIssuerSigningKey = true,
    ValidIssuer = jwtSettings["Issuer"],
    ValidAudience = jwtSettings["Audience"],
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!))
  };
});

builder.Services.AddAuthorization();

// 1.7. Swagger with JWT Support
builder.Services.AddSwaggerGen(c =>
{
  c.SwaggerDoc("v1", new OpenApiInfo { Title = "Employee API", Version = "v1" });
  c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
  {
    Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
    Name = "Authorization",
    In = ParameterLocation.Header,
    Type = SecuritySchemeType.ApiKey,
    Scheme = "Bearer"
  });
  c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

// 1.8. CORS Policy — M3-FIX: Read from config
var corsOrigins = builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:4200" };

builder.Services.AddCors(options =>
{
  options.AddPolicy("AllowAngularApp", policy =>
  {
    policy.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // Required for httpOnly cookie support
  });
});

// N1-FIX (v2): Real per-IP / per-user rate limiting via PartitionedRateLimiter
// ─────────────────────────────────────────────────────────────────────────────
// Previous implementation used AddFixedWindowLimiter which creates a SINGLE global
// counter shared by all traffic.  PartitionedRateLimiter partitions by user-ID
// (authenticated) or remote-IP (anonymous/public) so each principal gets their own
// independent bucket — true per-user AND per-IP enforcement.
// ─────────────────────────────────────────────────────────────────────────────
builder.Services.AddRateLimiter(options =>
{
  options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

  // Return a proper ApiResponse<T> JSON body + Retry-After header on every 429
  options.OnRejected = async (context, token) =>
  {
    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
    context.HttpContext.Response.ContentType = "application/json";

    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
      context.HttpContext.Response.Headers["Retry-After"] =
          ((int)retryAfter.TotalSeconds).ToString();

    await context.HttpContext.Response.WriteAsJsonAsync(
        new
        {
          succeeded = false,
          errorCode = "RATE_LIMIT_EXCEEDED",
          message = "Too many requests. Please slow down and try again.",
          data = (object?)null,
          errors = (List<string>?)null
        }, token);
  };

  var isTesting = builder.Environment.IsEnvironment("Testing");

  // Helper so partition key always falls back to a non-null string
  static string Ip(HttpContext ctx) =>
      ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";

  static string? UserId(HttpContext ctx) =>
      ctx.User?.FindFirstValue(ClaimTypes.NameIdentifier);

  // ── 1. AUTH ─────────────────────────────────────────────────────────────
  // Public endpoints (login, refresh-token): always partition by IP.
  // 5 attempts / minute / IP  — blocks password-spray / credential-stuffing.
  options.AddPolicy("auth", ctx =>
      RateLimitPartition.GetFixedWindowLimiter(
          partitionKey: $"auth:ip:{Ip(ctx)}",
          factory: _ => new FixedWindowRateLimiterOptions
          {
            PermitLimit = isTesting ? 1000 : 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
          }));

  // ── 2. CHECK-IN / CHECK-OUT ─────────────────────────────────────────────
  // Attendance endpoint hit once-twice a day per employee.
  // Authenticated → per-user  : 10 events / hour (generous: covers manual corrections)
  // Anonymous    → per-IP     : 5  events / hour (should never happen — endpoint requires auth)
  options.AddPolicy("checkin", ctx =>
  {
    var uid = UserId(ctx);
    return !string.IsNullOrEmpty(uid)
        ? RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"checkin:user:{uid}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
              PermitLimit = isTesting ? 1000 : 10,
              Window = TimeSpan.FromHours(1),
              QueueLimit = 0
            })
        : RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"checkin:ip:{Ip(ctx)}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
              PermitLimit = isTesting ? 1000 : 5,
              Window = TimeSpan.FromHours(1),
              QueueLimit = 0
            });
  });

  // ── 3. FILE UPLOAD ──────────────────────────────────────────────────────
  // Authenticated → per-user  : 20 uploads / hour
  // Anonymous    → per-IP     : 5  uploads / hour
  options.AddPolicy("file-upload", ctx =>
  {
    var uid = UserId(ctx);
    return !string.IsNullOrEmpty(uid)
        ? RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"file:user:{uid}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
              PermitLimit = isTesting ? 1000 : 20,
              Window = TimeSpan.FromHours(1),
              QueueLimit = 0
            })
        : RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"file:ip:{Ip(ctx)}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
              PermitLimit = isTesting ? 1000 : 5,
              Window = TimeSpan.FromHours(1),
              QueueLimit = 0
            });
  });

  // ── 4. WRITE (leave requests, HR mutations) ─────────────────────────────
  // Authenticated → per-user  : 30 mutations / minute
  // Anonymous    → per-IP     : 10 mutations / minute
  options.AddPolicy("write", ctx =>
  {
    var uid = UserId(ctx);
    return !string.IsNullOrEmpty(uid)
        ? RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"write:user:{uid}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
              PermitLimit = isTesting ? 1000 : 30,
              Window = TimeSpan.FromMinutes(1),
              QueueLimit = 0
            })
        : RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"write:ip:{Ip(ctx)}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
              PermitLimit = isTesting ? 1000 : 10,
              Window = TimeSpan.FromMinutes(1),
              QueueLimit = 0
            });
  });

  // ── 5. GENERAL (catch-all for every other API route) ────────────────────
  // Authenticated → per-user  : 200 req / minute  (covers normal SPA polling)
  // Anonymous    → per-IP     : 60  req / minute
  options.AddPolicy("general", ctx =>
  {
    var uid = UserId(ctx);
    return !string.IsNullOrEmpty(uid)
        ? RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"general:user:{uid}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
              PermitLimit = isTesting ? 1000 : 200,
              Window = TimeSpan.FromMinutes(1),
              QueueLimit = 10
            })
        : RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"general:ip:{Ip(ctx)}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
              PermitLimit = isTesting ? 1000 : 60,
              Window = TimeSpan.FromMinutes(1),
              QueueLimit = 5
            });
  });
});

// N2-FIX: Health Checks — MongoDB + self
builder.Services.AddHealthChecks()
    .AddMongoDb(
        name: "mongodb",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["db", "mongodb"]);

// N3-FIX: Response Compression & Caching
builder.Services.AddResponseCaching();
builder.Services.AddResponseCompression(options =>
{
  options.EnableForHttps = true;
  options.Providers.Add<BrotliCompressionProvider>();
  options.Providers.Add<GzipCompressionProvider>();
});

builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
  options.Level = CompressionLevel.Fastest;
});

builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
  options.Level = CompressionLevel.SmallestSize;
});

var app = builder.Build();

// =========================================================================
// 2. MIDDLEWARE PIPELINE
// =========================================================================

app.UseExceptionHandler();

// C5-FIX: HTTPS Redirect — upgrades HTTP requests to HTTPS in production
// HSTS: tell browsers to always use HTTPS for this domain (production only)
if (!app.Environment.IsDevelopment())
{
  app.UseHsts();
}
app.UseHttpsRedirection();

// C5-FIX: Security Headers — CSP, X-Frame-Options, X-Content-Type-Options, etc.
app.UseSecurityHeaders();

// NEW: Structured Request Logging
app.UseSerilogRequestLogging();

// 2.1. Data Seeding (Consolidated)
using (var scope = app.Services.CreateScope())
{
  var services = scope.ServiceProvider;
  var seedLogger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DataSeeding");
  try
  {
    // OPT-5: Initialize MongoDB Indexes
    var context = services.GetRequiredService<Employee.Infrastructure.Persistence.IMongoContext>();
    await Employee.Infrastructure.Data.MongoIndexInitializer.CreateIndexesAsync(context);

    // await DbSeeder.SeedUsersAndRolesAsync(services); // TEMPORARILY DISABLED
    seedLogger.LogInformation("Data Seeding Completed Successfully!");
  }
  catch (Exception ex)
  {
    seedLogger.LogError(ex, "An error occurred while seeding the database");
  }
}

// 2.2. Development Swagger
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
  app.UseSwagger();
  app.UseSwaggerUI();
}

// 2.3. Core Middleware
app.UseCors("AllowAngularApp");
app.UseResponseCompression(); // N3-FIX
app.UseResponseCaching(); // N3-FIX
app.UseRateLimiter(); // N1-FIX
app.UseAuthentication();
app.UseAuthorization();

// Protect uploaded files from anonymous access; public assets remain open
app.UseWhen(
    context => context.Request.Path.StartsWithSegments("/uploads")
               && !context.Request.Path.StartsWithSegments("/uploads/avatars")
               && !context.Request.Path.StartsWithSegments("/uploads/general"),
    branch => branch.Use(async (ctx, next) =>
    {
      if (ctx.User?.Identity?.IsAuthenticated != true)
      {
        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return;
      }
      await next();
    })
);
app.UseStaticFiles();

// 2.4. Map Endpoints (Carter)
// 2.4. Map Endpoints (Carter) with API Versioning
var versionSet = app.NewApiVersionSet()
    .HasApiVersion(new Asp.Versioning.ApiVersion(1))
    .ReportApiVersions()
    .Build();

// Map default /api routes (implicit v1) and associate with version set
// This enables "api-supported-versions" headers and swagger grouping
app.MapGroup("")
   .WithApiVersionSet(versionSet)
   .RequireRateLimiting("general")   // N1-FIX: apply rate limiter to all API routes
   .MapCarter();

// N2-FIX: Health Check endpoints
// Public liveness probe — safe for load balancers and Docker HEALTHCHECK (no internal info leaked)
app.MapHealthChecks("/health", new HealthCheckOptions
{
  ResponseWriter = async (context, report) =>
  {
    context.Response.ContentType = MediaTypeNames.Application.Json;
    var result = JsonSerializer.Serialize(new { status = report.Status.ToString() });
    await context.Response.WriteAsync(result);
  }
});

// Detailed health endpoint — Admin-only, exposes service names and check durations
app.MapHealthChecks("/health/detail", new HealthCheckOptions
{
  ResponseWriter = async (context, report) =>
  {
    context.Response.ContentType = MediaTypeNames.Application.Json;
    var result = JsonSerializer.Serialize(new
    {
      status = report.Status.ToString(),
      duration = report.TotalDuration.TotalMilliseconds,
      checks = report.Entries.Select(e => new
      {
        name = e.Key,
        status = e.Value.Status.ToString(),
        description = e.Value.Description,
        duration = e.Value.Duration.TotalMilliseconds
      })
    });
    await context.Response.WriteAsync(result);
  }
}).RequireAuthorization(policy => policy.RequireRole("Admin"));

app.Run();

// Required for WebApplicationFactory<Program> in integration tests
public partial class Program { }
