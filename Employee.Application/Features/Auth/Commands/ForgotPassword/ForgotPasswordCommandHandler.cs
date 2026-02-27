using Employee.Application.Common.Exceptions;
using Employee.Application.Common.Interfaces;
using MediatR;

namespace Employee.Application.Features.Auth.Commands.ForgotPassword
{
  public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, string>
  {
    private readonly IIdentityService _identityService;
    private readonly IEmailService _emailService;

    public ForgotPasswordCommandHandler(IIdentityService identityService, IEmailService emailService)
    {
      _identityService = identityService;
      _emailService = emailService;
    }

    public async Task<string> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
      var userDto = await _identityService.GetUserByEmailAsync(request.Email.Trim());
      // Trả về thông báo trung lập để tránh user enumeration (OWASP)
      // Không tiết lộ email có tồn tại trong hệ thống hay không
      if (userDto == null)
        return "Nếu email tồn tại trong hệ thống, mã đặt lại mật khẩu sẽ được gửi đến email của bạn.";

      // Generate reset token using IIdentityService
      var token = await _identityService.GenerateForgotPasswordTokenAsync(request.Email.Trim());

      // Send professional HTML email
      var subject = "🔒 Yêu cầu đặt lại mật khẩu - Employee HR System";
      var body = $@"
        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
          <h2 style='color: #2563eb;'>Đặt lại mật khẩu</h2>
          <p>Xin chào <strong>{userDto.FullName ?? userDto.Username}</strong>,</p>
          <p>Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn.</p>
          <p>Mã xác nhận của bạn:</p>
          <div style='background: #f3f4f6; padding: 15px; border-radius: 8px; font-size: 18px; font-weight: bold; text-align: center; letter-spacing: 2px;'>
            {token}
          </div>
          <p style='margin-top: 15px; color: #6b7280; font-size: 14px;'>
            ⚠️ Mã này sẽ hết hạn sau một thời gian. Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.
          </p>
          <hr style='border: none; border-top: 1px solid #e5e7eb; margin: 20px 0;' />
          <p style='color: #9ca3af; font-size: 12px;'>Employee HR System - Email tự động, vui lòng không trả lời.</p>
        </div>";

      await _emailService.SendAsync(request.Email.Trim(), subject, body, isHtml: true);

      return "Nếu email tồn tại trong hệ thống, mã đặt lại mật khẩu sẽ được gửi đến email của bạn.";
    }
  }
}
