using Employee.Application.Common.Interfaces.Organization.IRepository;
using Employee.Application.Common.Models;
using Employee.Application.Features.Organization.Dtos;
using Employee.Application.Features.Organization.Mappers;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Employee.Application.Features.Organization.Queries.GetPositionsPaged
{
  public class GetPositionsPagedQueryHandler(IPositionRepository repo) : IRequestHandler<GetPositionsPagedQuery, PagedResult<PositionDto>>
  {
    public async Task<PagedResult<PositionDto>> Handle(GetPositionsPagedQuery request, CancellationToken cancellationToken)
    {
      var paged = await repo.GetPagedAsync(request.Pagination, cancellationToken);

      // Get all parent IDs to fetch titles
      var parentIds = paged.Items.Where(x => !string.IsNullOrEmpty(x.ParentId)).Select(x => x.ParentId!).Distinct().ToList();
      var parentNames = await repo.GetNamesByIdsAsync(parentIds, cancellationToken);

      var dtos = paged.Items.Select(x =>
      {
        var dto = x.ToDto();
        if (!string.IsNullOrEmpty(x.ParentId) && parentNames.TryGetValue(x.ParentId, out var title))
        {
          dto.ParentTitle = title;
        }
        return dto;
      }).ToList();

      return new PagedResult<PositionDto>
      {
        Items = dtos,
        TotalCount = paged.TotalCount,
        PageNumber = paged.PageNumber,
        PageSize = paged.PageSize
      };
    }
  }
}
