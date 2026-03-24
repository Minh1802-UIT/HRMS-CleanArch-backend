using Employee.Application.Common.Security;
using Employee.Application.Features.Performance.Dtos;
using MediatR;

namespace Employee.Application.Features.Performance.Commands.CreatePIP
{
  [Authorize(Roles = "Admin,HR,Manager")]
  public record CreatePIPCommand(PIPDto Dto) : IRequest<string>;
}
