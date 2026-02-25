using Employee.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;

namespace Employee.Infrastructure.Services
{
  /// <summary>
  /// Production EmailService using SMTP.
  /// Supports any SMTP provider: Gmail, Outlook, SendGrid SMTP, AWS SES SMTP, etc.
  /// 
  /// Configuration via appsettings.json "EmailSettings" section:
  /// - SmtpHost: SMTP server address
  /// - SmtpPort: SMTP port (587 for TLS, 465 for SSL, 25 for unencrypted)
  /// - SenderEmail: From address
  /// - SenderName: Display name for From
  /// - Username: SMTP auth username (often same as SenderEmail)
  /// - Password: SMTP auth password (use App Password for Gmail)
  /// - EnableSsl: Whether to use TLS/SSL
  /// </summary>
  public class SmtpEmailService : IEmailService
  {
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
    {
      _configuration = configuration;
      _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string body, bool isHtml = false)
    {
      var emailSettings = _configuration.GetSection("EmailSettings");

      var smtpHost = emailSettings["SmtpHost"] ?? throw new InvalidOperationException("EmailSettings:SmtpHost is not configured.");
      var smtpPort = int.Parse(emailSettings["SmtpPort"] ?? "587");
      var senderEmail = emailSettings["SenderEmail"] ?? throw new InvalidOperationException("EmailSettings:SenderEmail is not configured.");
      var senderName = emailSettings["SenderName"] ?? "Employee HR System";
      var username = emailSettings["Username"] ?? senderEmail;
      var password = emailSettings["Password"] ?? throw new InvalidOperationException("EmailSettings:Password is not configured.");
      var enableSsl = bool.Parse(emailSettings["EnableSsl"] ?? "true");

      try
      {
        var mailMessage = new MailMessage
        {
          From = new MailAddress(senderEmail, senderName),
          Subject = subject,
          Body = body,
          IsBodyHtml = isHtml
        };
        mailMessage.To.Add(new MailAddress(to));

        using var smtpClient = new SmtpClient(smtpHost, smtpPort)
        {
          Credentials = new NetworkCredential(username, password),
          EnableSsl = enableSsl,
          DeliveryMethod = SmtpDeliveryMethod.Network,
          Timeout = 30000 // 30 seconds
        };

        await smtpClient.SendMailAsync(mailMessage);

        _logger.LogInformation("Email sent successfully to {To} with subject '{Subject}'", to, subject);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Failed to send email to {To} with subject '{Subject}'", to, subject);
        throw;
      }
    }
  }
}
