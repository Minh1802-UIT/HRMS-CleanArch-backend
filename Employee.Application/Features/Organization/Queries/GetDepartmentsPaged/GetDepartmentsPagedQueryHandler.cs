using Employee.Domain.Interfaces.Repositories;
using Employee.Domain.Common.Models;
using Employee.Application.Features.Organization.Dtos;
using Employee.Application.Features.Organization.Mappers;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Employee.Application.Features.Organization.Queries.GetDepartmentsPaged
{
  public class GetDepartmentsPagedQueryHandler(IDepartmentRepository repo) : IRequestHandler<GetDepartmentsPagedQuery, PagedResult<DepartmentDto>>
  {
    public async Task<PagedResult<DepartmentDto>> Handle(GetDepartmentsPagedQuery request, CancellationToken cancellationToken)
    {
      var paged = await repo.GetPagedAsync(request.Pagination, cancellationToken);
      return new PagedResult<DepartmentDto>
      {
        Items = paged.Items.Select(d => d.ToDto()!).ToList(),
        TotalCount = paged.TotalCount,
        PageNumber = paged.PageNumber,
        PageSize = paged.PageSize
      };
    }
  }
}
