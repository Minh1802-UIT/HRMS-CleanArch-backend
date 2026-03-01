using Employee.Application.Common.Security;
using Employee.Application.Features.Organization.Dtos;
using MediatR;

namespace Employee.Application.Features.Organization.Commands.CreatePosition
{
  [Authorize(Roles = "Admin,HR")]
public record CreatePositionCommand(CreatePositionDto Dto) : IRequest<string>;
}
