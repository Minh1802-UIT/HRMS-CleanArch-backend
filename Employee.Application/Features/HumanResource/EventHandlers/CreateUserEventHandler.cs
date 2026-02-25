using MediatR;
using Employee.Application.Common.Utils;
using Employee.Application.Common.Interfaces;
using Employee.Application.Features.Auth.Commands.Register;
using Employee.Application.Features.HumanResource.Events;

namespace Employee.Application.Features.HumanResource.EventHandlers
{
  public class CreateUserEventHandler : INotificationHandler<EmployeeCreatedEvent>
  {
    private readonly ISender _sender;
    private readonly IEmailService _emailService;

    public CreateUserEventHandler(ISender sender, IEmailService emailService)
    {
      _sender = sender;
      _emailService = emailService;
    }

    public async Task Handle(EmployeeCreatedEvent notification, CancellationToken cancellationToken)
    {
      var employee = notification.Employee;

      // 1. Generate a cryptographically random temporary password.
      //    MustChangePassword = true forces the user to set their own password on first login.
      var tempPassword = PasswordGenerator.Generate();

      await _sender.Send(new RegisterCommand
      {
        Username = employee.EmployeeCode,
        Email = employee.Email,
        FullName = employee.FullName,
        Password = tempPassword,
        EmployeeId = employee.Id,
        MustChangePassword = true
      }, cancellationToken);

      // 2. Email the temporary password so the employee can log in for the first time.
      //    The system will force a password change on first login.
      try
      {
        var subject = "🎉 Chào mừng đến HRMS - Thông tin tài khoản của bạn";
        var body = $@"
<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
  <h2 style='color: #2563eb;'>Chào mừng, {employee.FullName}!</h2>
  <p>Tài khoản của bạn đã được tạo trong hệ thống <strong>Employee HR System</strong>.</p>
  <table style='width:100%; border-collapse:collapse; margin: 20px 0;'>
    <tr>
      <td style='padding:8px; background:#f3f4f6; font-weight:bold; width:40%;'>Tên đăng nhập</td>
      <td style='padding:8px; background:#f3f4f6;'>{employee.EmployeeCode}</td>
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

        await _emailService.SendAsync(employee.Email, subject, body, isHtml: true);
      }
      catch
      {
        // Non-critical: account was created successfully.
        // Admin can trigger a password reset manually if email delivery fails.
      }
    }
  }
}
