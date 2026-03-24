using Employee.Application.Common.Security;
using Employee.Application.Features.Performance.Dtos;
using MediatR;

namespace Employee.Application.Features.Performance.Commands.UpdatePIPProgress
{
  [Authorize(Roles = "Admin,HR,Manager")]
  public record UpdatePIPProgressCommand(string Id, PIPUpdateProgressDto Dto) : IRequest<bool>;
}
