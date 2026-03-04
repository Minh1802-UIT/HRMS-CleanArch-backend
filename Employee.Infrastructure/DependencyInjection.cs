using Employee.Application.Common.Interfaces;
using Employee.Infrastructure.Persistence;
using Employee.Domain.Interfaces.Repositories;
using Employee.Infrastructure.data.Configurations;
using Employee.Infrastructure.Repositories.Attendance;
using Employee.Infrastructure.Repositories.Auth;
using Employee.Infrastructure.Repositories.Common;
using Employee.Infrastructure.Repositories.HumanResource;
using Employee.Infrastructure.Repositories.Leave;
using Employee.Infrastructure.Repositories.Organization;
using Employee.Infrastructure.Repositories.Payroll;
using Employee.Infrastructure.Repositories.Performance;
using Employee.Infrastructure.Services;
using Employee.Infrastructure.BackgroundServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using StackExchange.Redis;

namespace Employee.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // ==========================================
            // 1. DATABASE CONFIGURATION
            // ==========================================
            MongoClassMapConfig.Configure();

            var connectionString = configuration.GetValue<string>("EmployeeDatabaseSettings:ConnectionString");
            var databaseName = configuration.GetValue<string>("EmployeeDatabaseSettings:DatabaseName");

            services.AddSingleton<IMongoClient>(sp => new MongoClient(connectionString));
            services.AddScoped<IMongoContext>(sp =>
            {
                var client = sp.GetRequiredService<IMongoClient>();
                return new Employee.Infrastructure.Persistence.MongoContext(client, databaseName ?? "EmployeeDB");
            });
            services.AddScoped<IUnitOfWork, Employee.Infrastructure.Persistence.UnitOfWork>();

            // ==========================================
            // 1.1. REDIS CACHE CONFIGURATION
            // ==========================================
            var redisConnectionString = configuration.GetValue<string>("RedisSettings:ConnectionString");
            services.AddStackExchangeRedisCache(options =>
            {
                // ConfigurationOptions.Parse() does NOT understand "rediss://" URIs —
                // it treats the entire URI as a hostname, resulting in a doubled port
                // like "upstash.io:6379:6379" that can never resolve.
                // Upstash always provides "rediss://default:PASSWORD@host:port" so we
                // must parse the URI manually and set Ssl = true explicitly.
                var rawCs = redisConnectionString ?? "localhost:6379";
                ConfigurationOptions cfg;

                if (rawCs.StartsWith("rediss://", StringComparison.OrdinalIgnoreCase) ||
                    rawCs.StartsWith("redis://", StringComparison.OrdinalIgnoreCase))
                {
                    var uri = new Uri(rawCs);
                    var password = Uri.UnescapeDataString(uri.UserInfo.Split(':', 2).LastOrDefault() ?? "");
                    cfg = new ConfigurationOptions
                    {
                        EndPoints = { $"{uri.Host}:{uri.Port}" },
                        Password = password,
                        Ssl = uri.Scheme.Equals("rediss", StringComparison.OrdinalIgnoreCase),
                        SslProtocols = System.Security.Authentication.SslProtocols.Tls12
                                     | System.Security.Authentication.SslProtocols.Tls13,
                    };
                }
                else
                {
                    // Plain StackExchange.Redis connection string — Parse() works fine
                    cfg = ConfigurationOptions.Parse(rawCs);
                }

                cfg.ConnectTimeout = 1500;  // 1.5 s — abort connect attempt
                cfg.SyncTimeout = 1000;  // 1 s   — abort sync op
                cfg.AsyncTimeout = 1000;  // 1 s   — abort async op
                cfg.AbortOnConnectFail = false;  // stay up if Redis unreachable
                cfg.ReconnectRetryPolicy = new LinearRetry(2000);
                options.ConfigurationOptions = cfg;
                options.InstanceName = "HRM_";
            });

            // ==========================================
            // 2. SYSTEM & AUTHENTICATION REPOSITORIES
            // ==========================================
            services.AddSingleton<Employee.Domain.Interfaces.Common.IDateTimeProvider, Employee.Infrastructure.Services.DateTimeProvider>();
            services.AddScoped<IAuditLogRepository, AuditLogRepository>();
            services.AddScoped<ISystemSettingRepository, SystemSettingRepository>();
            services.AddScoped<IPasswordHasher, PasswordHasher>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IIdentityService, Employee.Infrastructure.Identity.IdentityService>();

            // ==========================================
            // 3. ORGANIZATION REPOSITORIES
            // ==========================================
            services.AddScoped<IDepartmentRepository, DepartmentRepository>();
            services.AddScoped<IPositionRepository, PositionRepository>();

            // ==========================================
            // 4. HUMAN RESOURCES REPOSITORIES
            // ==========================================
            services.AddScoped<IEmployeeRepository, EmployeeRepository>();
            services.AddScoped<IEmployeeQueryRepository, EmployeeRepository>();
            services.AddScoped<IContractRepository, ContractRepository>();
            services.AddScoped<IContractQueryRepository, ContractRepository>();
            services.AddScoped<IJobVacancyRepository, JobVacancyRepository>();
            services.AddScoped<ICandidateRepository, CandidateRepository>();
            services.AddScoped<IInterviewRepository, InterviewRepository>();

            // ==========================================
            // 5. ATTENDANCE REPOSITORIES
            // ==========================================
            services.AddScoped<IShiftRepository, ShiftRepository>();
            services.AddScoped<IRawAttendanceLogRepository, Employee.Infrastructure.Repositories.Attendance.RawAttendanceLogRepository>();
            services.AddScoped<IAttendanceRepository, Employee.Infrastructure.Repositories.Attendance.AttendanceRepository>();

            // ==========================================
            // 6. LEAVE MANAGEMENT REPOSITORIES
            // ==========================================
            services.AddScoped<ILeaveTypeRepository, LeaveTypeRepository>();
            services.AddScoped<ILeaveAllocationRepository, LeaveAllocationRepository>();
            services.AddScoped<ILeaveRequestRepository, LeaveRequestRepository>();

            // ==========================================
            // 6b. NOTIFICATION REPOSITORY (NEW-9)
            // ==========================================
            services.AddScoped<INotificationRepository, Employee.Infrastructure.Repositories.Notifications.NotificationRepository>();

            // ==========================================
            // 7. PAYROLL REPOSITORIES
            // ==========================================
            services.AddScoped<IPayrollRepository, PayrollRepository>();
            services.AddScoped<IPublicHolidayRepository, PublicHolidayRepository>();
            services.AddScoped<IPayrollCycleRepository, PayrollCycleRepository>();

            // ==========================================
            // 8. PERFORMANCE REPOSITORIES
            // ==========================================
            services.AddScoped<IPerformanceReviewRepository, PerformanceReviewRepository>();
            services.AddScoped<IPerformanceGoalRepository, PerformanceGoalRepository>();

            // ==========================================
            // 9. BACKGROUND SERVICES
            // ==========================================
            services.AddHostedService<LeaveAccrualBackgroundService>();
            services.AddHostedService<PayrollBackgroundService>();
            services.AddHostedService<ContractExpirationBackgroundService>();
            // Nightly hard-delete of soft-deleted records older than 90 days
            services.AddHostedService<SoftDeleteCleanupBackgroundService>();

            // ==========================================
            // 9. INFRASTRUCTURE SERVICES
            // ==========================================
            services.Configure<SupabaseStorageOptions>(configuration.GetSection(SupabaseStorageOptions.SectionName));
            services.AddHttpClient("SupabaseStorage");
            services.AddScoped<IFileService, SupabaseFileService>();
            services.AddScoped<ICacheService, CacheService>();

            return services;
        }

        public static IServiceCollection AddEmailService(this IServiceCollection services, IConfiguration configuration, bool isDevelopment)
        {
            var sendGridKey = configuration["EmailSettings:SendGridApiKey"];
            if (string.IsNullOrEmpty(sendGridKey) || sendGridKey == "OVERRIDE_VIA_USER_SECRETS_OR_ENV")
            {
                sendGridKey = Environment.GetEnvironmentVariable("EmailSettings__SendGridApiKey") ?? Environment.GetEnvironmentVariable("SendGridApiKey");
            }

            var smtpPassword = configuration["EmailSettings:Password"];
            if (string.IsNullOrEmpty(smtpPassword) || smtpPassword == "OVERRIDE_VIA_USER_SECRETS_OR_ENV")
            {
                smtpPassword = Environment.GetEnvironmentVariable("EmailSettings__Password") ?? Environment.GetEnvironmentVariable("Password");
            }

            bool hasSendGrid = !string.IsNullOrWhiteSpace(sendGridKey) && sendGridKey != "OVERRIDE_VIA_USER_SECRETS_OR_ENV";
            bool hasSmtp = !string.IsNullOrWhiteSpace(smtpPassword) && smtpPassword != "OVERRIDE_VIA_USER_SECRETS_OR_ENV";

            if (hasSendGrid)
            {
                services.AddScoped<IEmailService, SendGridEmailService>();
            }
            else if (hasSmtp)
            {
                services.AddScoped<IEmailService, SmtpEmailService>();
            }
            else
            {
                services.AddScoped<IEmailService, DevEmailService>();
            }

            return services;
        }
    }
}
