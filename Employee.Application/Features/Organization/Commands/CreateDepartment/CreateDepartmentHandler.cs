using Employee.Application.Common;
using Employee.Application.Common.Exceptions;
using Employee.Application.Common.Interfaces;
using Employee.Domain.Interfaces.Repositories;
using Employee.Application.Features.Organization.Dtos;
using Employee.Application.Features.Organization.Mappers;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Employee.Application.Features.Organization.Commands.CreateDepartment
{
  public class CreateDepartmentHandler(IDepartmentRepository repo, ICacheService cache) : IRequestHandler<CreateDepartmentCommand, DepartmentDto>
  {
    private const int MaxDepth = 10;

    public async Task<DepartmentDto> Handle(CreateDepartmentCommand request, CancellationToken cancellationToken)
    {
      if (!string.IsNullOrEmpty(request.Dto.ParentId))
      {
        var parent = await repo.GetByIdAsync(request.Dto.ParentId, cancellationToken);
        if (parent == null) throw new NotFoundException($"Parent department {request.Dto.ParentId} not found");

        // Guard: prevent excessively deep hierarchy
        var depth = await GetDepthAsync(request.Dto.ParentId, cancellationToken);
        if (depth >= MaxDepth)
          throw new ValidationException($"Department hierarchy cannot exceed {MaxDepth} levels.");
      }

      var dept = request.Dto.ToEntity();

      // Belt-and-suspenders: generated Id must never equal ParentId
      if (!string.IsNullOrEmpty(dept.Id) && dept.Id == dept.ParentId)
        throw new ValidationException("A department cannot be its own parent.");

      await repo.CreateAsync(dept, cancellationToken);
      await cache.RemoveAsync(CacheKeys.DepartmentTree);
      return dept.ToDto()!;
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
