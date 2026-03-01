using Employee.Application.Common.Security;
using MediatR;

namespace Employee.Application.Features.Organization.Commands.DeletePosition
{
  [Authorize(Roles = "Admin")]
public record DeletePositionCommand(string Id) : IRequest;
}
