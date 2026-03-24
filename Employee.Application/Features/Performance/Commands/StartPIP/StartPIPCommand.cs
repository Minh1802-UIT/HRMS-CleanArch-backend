using Employee.Application.Common.Security;
using MediatR;

namespace Employee.Application.Features.Performance.Commands.StartPIP
{
  [Authorize(Roles = "Admin,HR,Manager")]
  public record StartPIPCommand(string Id) : IRequest<bool>;
}
