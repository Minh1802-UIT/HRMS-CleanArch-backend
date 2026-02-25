using MediatR;

namespace Employee.Application.Features.Organization.Commands.DeleteDepartment
{
  public record DeleteDepartmentCommand(string Id) : IRequest;
}
