using AspNetCore.Identity.MongoDbCore.Extensions;
using AspNetCore.Identity.MongoDbCore.Infrastructure;
using Microsoft.AspNetCore.Identity;
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
using System.Reflection;
using Hangfire;
using Serilog;
using Microsoft.AspNetCore.HttpOverrides;
using OpenTelemetry.Trace;

MongoClassMapConfig.Configure();

var builder = WebApplication.CreateBuilder(args);

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

// 1.2. OpenTelemetry - Tracing
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource("Hangfire")
        .SetSampler(new OpenTelemetry.Trace.AlwaysOnSampler())
        .AddConsoleExporter());

// Enforce camelCase JSON globally for ALL responses (Results.Ok, Results.Json, WriteAsJsonAsync)
builder.Services.ConfigureHttpJsonOptions(options =>
{
  options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
  options.SerializerOptions.DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

// API Versioning
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

// Validate JWT Key at startup — fail fast if missing or still using a placeholder.
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
    // Password policy for the HR system
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
builder.Services.ConfigureMongoDbIdentity<ApplicationUser, ApplicationRole, Guid>(mongoConfig)
    .AddDefaultTokenProviders();

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
  c.SwaggerDoc("v1", new OpenApiInfo 
  { 
    Title = "HRMS API", 
    Version = "v1",
    Description = "Human Resources Management System API\n\n## Features\n- Authentication & Authorization\n- Employee Management\n- Leave Management\n- Attendance Tracking\n- Payroll Processing\n- Performance Management\n- Recruitment",
    Contact = new OpenApiContact 
    {
      Name = "HRMS Team",
      Email = "support@hrms.com"
    },
    License = new OpenApiLicense 
    {
      Name = "MIT"
    }
  });
  
  // XML Comments
  var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
  var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
  if (File.Exists(xmlPath))
  {
    c.IncludeXmlComments(xmlPath);
  }
  
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

// 1.8. CORS Policy — allowed origins are read from configuration
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

// Per-IP (anonymous) and per-user (authenticated) rate limiting via PartitionedRateLimiter.
// Each principal gets its own independent bucket.
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

  // ── 1a. AUTH — login / register ─────────────────────────────────────────
  // Password-spray protection: 10 attempts / minute / real-client-IP.
  // Raised from 5 → 10 because on Render.com the ForwardedHeaders middleware
  // now supplies the genuine client IP so the bucket is per-user, not global.
  options.AddPolicy("auth", ctx =>
      RateLimitPartition.GetFixedWindowLimiter(
          partitionKey: $"auth:ip:{Ip(ctx)}",
          factory: _ => new FixedWindowRateLimiterOptions
          {
            PermitLimit = isTesting ? 1000 : 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
          }));

  // ── 1b. REFRESH-TOKEN ────────────────────────────────────────────────────
  // Silent refresh runs automatically in the background (e.g. after F5).
  // It must NOT be throttled as aggressively as login — each page load may
  // legitimately trigger one refresh call.  30 calls / 5 minutes / IP.
  options.AddPolicy("refresh", ctx =>
      RateLimitPartition.GetFixedWindowLimiter(
          partitionKey: $"refresh:ip:{Ip(ctx)}",
          factory: _ => new FixedWindowRateLimiterOptions
          {
            PermitLimit = isTesting ? 1000 : 30,
            Window = TimeSpan.FromMinutes(5),
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

// Health Checks — MongoDB + Redis + self
var redisConnectionString = builder.Configuration.GetValue<string>("RedisSettings:ConnectionString") ?? "localhost:6379";
builder.Services.AddHealthChecks()
    .AddMongoDb(
        name: "mongodb",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["db", "mongodb"])
    .AddRedis(
        redisConnectionString,
        name: "redis",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["cache", "redis"]);

// Response Compression & Caching
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

// Trust the reverse proxy headers on Render.com so RemoteIpAddress = real client IP
// (without this, every request looks like it comes from the same load-balancer IP
// and all users share ONE rate-limit bucket, causing false 429s)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
  options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
  // Clear the default whitelist so we trust any upstream proxy Render.com may use
  options.KnownNetworks.Clear();
  options.KnownProxies.Clear();
});

var app = builder.Build();

// =========================================================================
// 2. MIDDLEWARE PIPELINE
// =========================================================================

// MUST be the very first middleware so RemoteIpAddress is already patched
// before the rate limiter (and everything else) reads it.
app.UseForwardedHeaders();

app.UseExceptionHandler();

// HSTS + HTTPS Redirect — production only
if (!app.Environment.IsDevelopment())
{
  app.UseHsts();
}
app.UseHttpsRedirection();

// Security headers — CSP, X-Frame-Options, X-Content-Type-Options, etc.
app.UseSecurityHeaders();

app.UseSerilogRequestLogging();

// 2.1. Data Seeding (Consolidated)
using (var scope = app.Services.CreateScope())
{
  var services = scope.ServiceProvider;
  var seedLogger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DataSeeding");
  try
  {
    // Initialize MongoDB indexes on startup
    var context = services.GetRequiredService<Employee.Infrastructure.Persistence.IMongoContext>();
    await Employee.Infrastructure.Data.MongoIndexInitializer.CreateIndexesAsync(context);

    seedLogger.LogInformation("Startup initialization completed.");
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
app.UseResponseCompression();
app.UseResponseCaching();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Hangfire dashboard — Admin-only, restricted to /hangfire path
// Skipped in Testing environment (no Redis connection available)
if (!app.Environment.IsEnvironment("Testing"))
{
  app.UseHangfireDashboard("/hangfire", new Hangfire.DashboardOptions
  {
    Authorization = new[] { new Employee.API.Services.HangfireAuthFilter() }
  });
}

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
   .RequireRateLimiting("general")
   .MapCarter();

// Public liveness probe — safe for load balancers and Docker HEALTHCHECK
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
