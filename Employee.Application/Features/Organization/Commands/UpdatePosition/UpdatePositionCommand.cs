using Employee.Application.Features.Organization.Dtos;
using MediatR;

namespace Employee.Application.Features.Organization.Commands.UpdatePosition
{
  public record UpdatePositionCommand(string Id, UpdatePositionDto Dto) : IRequest;
}
