using Employee.Application.Common.Interfaces;
using Employee.Application.Common.Utils;
using Employee.Application.Features.Auth.Commands.Register;
using Hangfire;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Employee.Infrastructure.Services
{
  /// <summary>
  /// Hangfire job that creates an Identity account for a newly-hired employee
  /// and sends a welcome email with a temporary password.
  ///
  /// Decorated with [AutomaticRetry] so Hangfire retries on transient failures
  /// (network lag, Identity server restart) with exponential back-off up to 5 attempts.
  /// This replaces the fragile fire-and-forget Task.Run in CreateUserEventHandler.
  /// </summary>
  public class AccountProvisioningJob
  {
    private readonly ISender _sender;
    private readonly IEmailService _emailService;
    private readonly ILogger<AccountProvisioningJob> _logger;

    public AccountProvisioningJob(
        ISender sender,
        IEmailService emailService,
        ILogger<AccountProvisioningJob> logger)
    {
      _sender = sender;
      _emailService = emailService;
      _logger = logger;
    }

    [AutomaticRetry(Attempts = 5, DelaysInSeconds = new[] { 60, 300, 600, 1800, 3600 })]
    public async Task ExecuteAsync(string employeeId, string email, string fullName, string phone)
    {
      _logger.LogInformation("Provisioning account for employee {EmployeeId} ({Email}).", employeeId, email);

      var tempPassword = PasswordGenerator.Generate();

      await _sender.Send(new RegisterCommand
      {
        Username = employeeId,
        Email = email,
        FullName = fullName,
        Password = tempPassword,
        EmployeeId = employeeId,
        MustChangePassword = true
      });

      var subject = "🎉 Chào mừng đến HRMS - Thông tin tài khoản của bạn";
      var body = $@"
<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
  <h2 style='color: #2563eb;'>Chào mừng, {fullName}!</h2>
  <p>Tài khoản của bạn đã được tạo trong hệ thống <strong>Employee HR System</strong>.</p>
  <table style='width:100%; border-collapse:collapse; margin: 20px 0;'>
    <tr>
      <td style='padding:8px; background:#f3f4f6; font-weight:bold; width:40%;'>Tên đăng nhập</td>
      <td style='padding:8px; background:#f3f4f6;'>{email}</td>
    </tr>
    <tr>
      <td style='padding:8px; font-weight:bold;'>Mật khẩu tạm thời</td>
      <td style='padding:8px; font-family:monospace; font-size:16px; font-weight:bold; color:#dc2626;'>{tempPassword}</td>
    </tr>
  </table>
  <p style='background:#fef3c7; padding:12px; border-radius:6px; border-left:4px solid #f59e0b;'>
    ⚠️ <strong>Bạn sẽ được yêu cầu đổi mật khẩu khi đăng nhập lần đầu tiên.</strong><br/>
    Vui lòng không chia sẻ mật khẩu này với bất kỳ ai.
  </p>
  <hr style='border: none; border-top: 1px solid #e5e7eb; margin: 20px 0;' />
  <p style='color: #9ca3af; font-size: 12px;'>Employee HR System — Email tự động, vui lòng không trả lời.</p>
</div>";

      await _emailService.SendAsync(email, subject, body, isHtml: true);

      _logger.LogInformation("Account provisioning completed for employee {EmployeeId}.", employeeId);
    }
  }
}
