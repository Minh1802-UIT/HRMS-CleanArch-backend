using Employee.Domain.Common.Models;
using Employee.Application.Features.HumanResource.Dtos;
using MediatR;

namespace Employee.Application.Features.HumanResource.Queries.GetEmployeeById
{
  public record GetEmployeeByIdQuery(string Id) : IRequest<EmployeeDto>;
}
