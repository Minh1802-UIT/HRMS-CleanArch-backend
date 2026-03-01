using Employee.Application.Common;
using Employee.Application.Common.Exceptions;
using Employee.Application.Common.Interfaces;
using Employee.Domain.Interfaces.Repositories;
using Employee.Application.Features.Organization.Mappers;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Employee.Application.Features.Organization.Commands.CreatePosition
{
  public class CreatePositionHandler(
      IPositionRepository repo,
      ICacheService cache,
      IDepartmentRepository deptRepo) : IRequestHandler<CreatePositionCommand, string>
  {
    private const int MaxDepth = 10;

    public async Task<string> Handle(CreatePositionCommand request, CancellationToken cancellationToken)
    {
      if (!string.IsNullOrEmpty(request.Dto.ParentId))
      {
        var parent = await repo.GetByIdAsync(request.Dto.ParentId, cancellationToken);
        if (parent == null) throw new NotFoundException("Không tìm thấy chức vụ cấp trên");

        // Guard: prevent excessively deep hierarchy
        var depth = await GetDepthAsync(request.Dto.ParentId, cancellationToken);
        if (depth >= MaxDepth)
          throw new ValidationException($"Position hierarchy cannot exceed {MaxDepth} levels.");
      }

      // Validate Department
      var dept = await deptRepo.GetByIdAsync(request.Dto.DepartmentId, cancellationToken);
      if (dept == null) throw new NotFoundException($"Department with ID {request.Dto.DepartmentId} not found.");

      var pos = request.Dto.ToEntity();

      // Belt-and-suspenders: generated Id must never equal ParentId
      if (!string.IsNullOrEmpty(pos.Id) && pos.Id == pos.ParentId)
        throw new ValidationException("A position cannot be its own parent.");

      await repo.CreateAsync(pos, cancellationToken);
      await cache.RemoveAsync(CacheKeys.PositionTree);
      return pos.Id;
    }

    /// <summary>Count how many ancestors the given node has (cycle-safe with visited set).</summary>
    private async Task<int> GetDepthAsync(string nodeId, CancellationToken ct)
    {
      var visited = new System.Collections.Generic.HashSet<string>();
      int depth = 0;
      var current = nodeId;
      while (!string.IsNullOrEmpty(current))
      {
        if (!visited.Add(current)) break; // cycle detected — stop counting
        var node = await repo.GetByIdAsync(current, ct);
        if (node == null) break;
        depth++;
        current = node.ParentId;
      }
      return depth;
    }
  }
}
