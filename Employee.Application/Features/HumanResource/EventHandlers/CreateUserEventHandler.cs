using Employee.Application.Common.Models;
using MediatR;
using Employee.Application.Common.Utils;
using Employee.Application.Common.Interfaces;
using Employee.Application.Features.Auth.Commands.Register;
using Employee.Domain.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Employee.Application.Features.HumanResource.EventHandlers
{
    public class CreateUserEventHandler : INotificationHandler<DomainEventNotification<EmployeeCreatedEvent>>
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<CreateUserEventHandler> _logger;

        public CreateUserEventHandler(IServiceScopeFactory scopeFactory, ILogger<CreateUserEventHandler> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public Task Handle(DomainEventNotification<EmployeeCreatedEvent> notificationWrapper, CancellationToken cancellationToken)
        {
            var evt = notificationWrapper.DomainEvent;

            // Xử lý tạo tài khoản và gửi email ngầm (Fire-and-forget)
            // Để API trả về 201 Created ngay lập tức mà không phải chờ SMTP server
            _ = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                var tempPassword = PasswordGenerator.Generate();

                try
                {
                    await sender.Send(new RegisterCommand
                    {
                        Username = evt.EmployeeId,  // Will be updated by identity service
                        Email = evt.Email,
                        FullName = evt.FullName,
                        Password = tempPassword,
                        EmployeeId = evt.EmployeeId,
                        MustChangePassword = true
                    });

                    var subject = "🎉 Chào mừng đến HRMS - Thông tin tài khoản của bạn";
                    var body = $@"
<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
  <h2 style='color: #2563eb;'>Chào mừng, {evt.FullName}!</h2>
  <p>Tài khoản của bạn đã được tạo trong hệ thống <strong>Employee HR System</strong>.</p>
  <table style='width:100%; border-collapse:collapse; margin: 20px 0;'>
    <tr>
      <td style='padding:8px; background:#f3f4f6; font-weight:bold; width:40%;'>Tên đăng nhập</td>
      <td style='padding:8px; background:#f3f4f6;'>{evt.Email}</td>
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

                    await emailService.SendAsync(evt.Email, subject, body, isHtml: true);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                  "Tạo tài khoản hoặc gửi email chào mừng thất bại cho nhân viên (ID: {EmployeeId}). " +
                  "Nhân viên đã được tạo nhưng chưa có tài khoản hệ thống.",
                  evt.EmployeeId);
                }
            });

            return Task.CompletedTask;
        }
    }
}

