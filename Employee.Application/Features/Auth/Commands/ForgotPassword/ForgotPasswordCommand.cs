using MediatR;

namespace Employee.Application.Features.Auth.Commands.ForgotPassword
{
  public class ForgotPasswordCommand : IRequest<string>
  {
    public string Email { get; set; } = string.Empty;
  }
}
