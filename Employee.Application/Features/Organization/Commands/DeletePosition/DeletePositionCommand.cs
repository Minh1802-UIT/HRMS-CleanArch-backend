using MediatR;

namespace Employee.Application.Features.Organization.Commands.DeletePosition
{
  public record DeletePositionCommand(string Id) : IRequest;
}
