using MediatR;
using Employee.Application.Common.Utils;
using Employee.Application.Features.Auth.Commands.Register;
using Employee.Application.Features.HumanResource.Events;

namespace Employee.Application.Features.HumanResource.EventHandlers
{
  public class CreateUserEventHandler : INotificationHandler<EmployeeCreatedEvent>
  {
    private readonly ISender _sender;

    public CreateUserEventHandler(ISender sender)
    {
      _sender = sender;
    }

    public async Task Handle(EmployeeCreatedEvent notification, CancellationToken cancellationToken)
    {
      var employee = notification.Employee;

      // Generate a cryptographically random temporary password.
      // MustChangePassword = true forces the user to set their own password on first login.
      await _sender.Send(new RegisterCommand
      {
        Username = employee.EmployeeCode,
        Email = employee.Email,
        FullName = employee.FullName,
        Password = PasswordGenerator.Generate(),
        EmployeeId = employee.Id,
        MustChangePassword = true
      });
    }
  }
}
