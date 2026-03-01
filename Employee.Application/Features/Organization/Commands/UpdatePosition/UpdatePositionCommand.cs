using Employee.Application.Common.Security;
using Employee.Application.Features.Organization.Dtos;
using MediatR;

namespace Employee.Application.Features.Organization.Commands.UpdatePosition
{
  [Authorize(Roles = "Admin,HR")]
public record UpdatePositionCommand(string Id, UpdatePositionDto Dto) : IRequest;
}
