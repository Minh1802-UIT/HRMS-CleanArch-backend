using Employee.Application.Common.Security;
using Employee.Application.Features.Performance.Dtos;
using MediatR;

namespace Employee.Application.Features.Performance.Commands.CompletePIP
{
  [Authorize(Roles = "Admin,HR,Manager")]
  public record CompletePIPCommand(string Id, PIPCompleteDto Dto) : IRequest<bool>;
}
