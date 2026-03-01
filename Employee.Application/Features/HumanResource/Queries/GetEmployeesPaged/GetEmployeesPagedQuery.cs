using Employee.Domain.Common.Models;
using Employee.Application.Features.HumanResource.Dtos;
using MediatR;

namespace Employee.Application.Features.HumanResource.Queries.GetEmployeesPaged
{
  public record GetEmployeesPagedQuery(PaginationParams Pagination) : IRequest<PagedResult<EmployeeListDto>>;
}
