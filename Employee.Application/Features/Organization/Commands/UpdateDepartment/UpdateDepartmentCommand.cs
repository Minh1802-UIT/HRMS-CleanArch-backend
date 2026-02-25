using Employee.Application.Features.Organization.Dtos;
using MediatR;

namespace Employee.Application.Features.Organization.Commands.UpdateDepartment
{
  public record UpdateDepartmentCommand(string Id, UpdateDepartmentDto Dto) : IRequest;
}
