using Employee.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Employee.Infrastructure.Services
{
  /// <summary>
  /// Production EmailService using SendGrid HTTP API.
  /// Bypasses Render's blocked SMTP ports (25, 465, 587) by using port 443 (HTTPS).
  /// 
  /// Configuration via appsettings.json "EmailSettings" section:
  /// - SendGridApiKey: SendGrid API key
  /// - SenderEmail: From address (must be verified in SendGrid)
  /// - SenderName: Display name for From
  /// </summary>
  public class SendGridEmailService : IEmailService
  {
    private readonly IConfiguration _configuration;
    private readonly ILogger<SendGridEmailService> _logger;

    public SendGridEmailService(IConfiguration configuration, ILogger<SendGridEmailService> logger)
    {
      _configuration = configuration;
      _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string body, bool isHtml = false)
    {
      var emailSettings = _configuration.GetSection("EmailSettings");
      
      var apiKey = emailSettings["SendGridApiKey"] ?? Environment.GetEnvironmentVariable("SendGridApiKey");
      if (string.IsNullOrEmpty(apiKey) || apiKey == "OVERRIDE_VIA_USER_SECRETS_OR_ENV")
      {
        _logger.LogWarning("SendGridApiKey is not configured. Email to {To} was not sent.", to);
        return;
      }

      var senderEmail = emailSettings["SenderEmail"] ?? throw new InvalidOperationException("EmailSettings:SenderEmail is not configured.");
      var senderName = emailSettings["SenderName"] ?? "Employee HR System";

      try
      {
        var client = new SendGridClient(apiKey);
        var from = new EmailAddress(senderEmail, senderName);
        var toAddress = new EmailAddress(to);
        
        var plainTextContent = isHtml ? null : body;
        var htmlContent = isHtml ? body : null;
        
        var msg = MailHelper.CreateSingleEmail(from, toAddress, subject, plainTextContent, htmlContent);
        
        var response = await client.SendEmailAsync(msg);

        if (response.IsSuccessStatusCode)
        {
          _logger.LogInformation("Email sent successfully to {To} with subject '{Subject}' via SendGrid", to, subject);
        }
        else
        {
          var responseBody = await response.Body.ReadAsStringAsync();
          _logger.LogError("Failed to send email to {To} with subject '{Subject}'. SendGrid returned {StatusCode}: {ResponseBody}", to, subject, response.StatusCode, responseBody);
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Exception occurred while sending email to {To} with subject '{Subject}' via SendGrid", to, subject);
        throw;
      }
    }
  }
}
