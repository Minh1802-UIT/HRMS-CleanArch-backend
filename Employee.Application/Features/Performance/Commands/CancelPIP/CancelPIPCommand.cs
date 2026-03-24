using Employee.Application.Common.Security;
using MediatR;

namespace Employee.Application.Features.Performance.Commands.CancelPIP
{
  [Authorize(Roles = "Admin,HR,Manager")]
  public record CancelPIPCommand(string Id, string Reason) : IRequest<bool>;
}
