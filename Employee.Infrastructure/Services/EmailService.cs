using Employee.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Employee.Infrastructure.Services
{
  /// <summary>
  /// Development-only EmailService that logs email content instead of sending.
  /// Used in Development environment to avoid requiring SMTP configuration.
  /// </summary>
  public class DevEmailService : IEmailService
  {
    private readonly ILogger<DevEmailService> _logger;

    public DevEmailService(ILogger<DevEmailService> logger)
    {
      _logger = logger;
    }

    public Task SendAsync(string to, string subject, string body, bool isHtml = false)
    {
      _logger.LogWarning("[DEV EMAIL] To: {To}, Subject: {Subject}, Body: {Body}", to, subject, body);
      return Task.CompletedTask;
    }
  }
}

