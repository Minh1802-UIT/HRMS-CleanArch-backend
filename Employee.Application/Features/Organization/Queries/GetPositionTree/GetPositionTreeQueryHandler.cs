using Employee.Application.Common.Interfaces;
using Employee.Domain.Interfaces.Repositories;
using Employee.Application.Features.Organization.Dtos;
using Employee.Domain.Entities.Organization;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Employee.Application.Features.Organization.Queries.GetPositionTree
{
  public class GetPositionTreeQueryHandler(IPositionRepository repo, ICacheService cache) : IRequestHandler<GetPositionTreeQuery, List<PositionNodeDto>>
  {
    private const string POSITION_TREE_KEY = "POSITION_TREE";

    public async Task<List<PositionNodeDto>> Handle(GetPositionTreeQuery request, CancellationToken cancellationToken)
    {
      // 1. Check Cache
      var cachedTree = await cache.GetAsync<List<PositionNodeDto>>(POSITION_TREE_KEY);
      if (cachedTree != null) return cachedTree;

      // 2. If not in cache, get from DB
      var allPositions = await repo.GetAllActiveAsync(cancellationToken);
      var rootPositions = allPositions.Where(x => string.IsNullOrEmpty(x.ParentId)).ToList();

      var result = new List<PositionNodeDto>();
      foreach (var root in rootPositions)
      {
        result.Add(BuildNode(root, allPositions));
      }

      // 3. Save to Cache (1 hour)
      await cache.SetAsync(POSITION_TREE_KEY, result, TimeSpan.FromHours(1));

      return result;
    }

    private PositionNodeDto BuildNode(Position pos, List<Position> allPositions)
    {
      var node = new PositionNodeDto
      {
        Id = pos.Id,
        Title = pos.Title,
        Code = pos.Code,
        DepartmentId = pos.DepartmentId
      };

      var children = allPositions.Where(x => x.ParentId == pos.Id).ToList();
      foreach (var child in children)
      {
        node.Children.Add(BuildNode(child, allPositions));
      }

      return node;
    }
  }
}
