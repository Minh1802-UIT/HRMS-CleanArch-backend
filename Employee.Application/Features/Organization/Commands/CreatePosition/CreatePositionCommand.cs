using Employee.Application.Features.Organization.Dtos;
using MediatR;

namespace Employee.Application.Features.Organization.Commands.CreatePosition
{
  public record CreatePositionCommand(CreatePositionDto Dto) : IRequest<string>;
}
