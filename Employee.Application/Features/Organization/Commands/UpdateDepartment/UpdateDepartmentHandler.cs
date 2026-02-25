using Employee.Application.Common.Exceptions;
using Employee.Application.Common.Interfaces;
using Employee.Application.Common.Interfaces.Organization.IRepository;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Employee.Application.Features.Organization.Commands.UpdateDepartment
{
  public class UpdateDepartmentHandler(IDepartmentRepository repo, ICacheService cache) : IRequestHandler<UpdateDepartmentCommand>
  {
    private const string DEPARTMENT_TREE_KEY = "DEPARTMENT_TREE";

    public async Task Handle(UpdateDepartmentCommand request, CancellationToken cancellationToken)
    {
      var dept = await repo.GetByIdAsync(request.Id, cancellationToken);
      if (dept == null) throw new NotFoundException($"Department {request.Id} not found");

      // 1. Cycle Detection
      if (!string.IsNullOrEmpty(request.Dto.ParentId))
      {
        if (request.Dto.ParentId == request.Id) throw new ValidationException("A department cannot be its own parent.");

        // Check if ParentId is a descendant of the current department
        var isDescendant = await IsDescendantAsync(request.Id, request.Dto.ParentId, cancellationToken);
        if (isDescendant) throw new ValidationException("A department cannot be a child of its own sub-tree.");

        var parent = await repo.GetByIdAsync(request.Dto.ParentId, cancellationToken);
        if (parent == null) throw new NotFoundException($"Parent department {request.Dto.ParentId} not found");
      }

      // Manual Map for Update
      dept.UpdateInfo(request.Dto.Name ?? dept.Name, request.Dto.Description ?? dept.Description);
      dept.AssignManager(request.Dto.ManagerId);
      dept.SetParent(request.Dto.ParentId);

      await repo.UpdateAsync(request.Id, dept, cancellationToken);
      await cache.RemoveAsync(DEPARTMENT_TREE_KEY);
    }

    private async Task<bool> IsDescendantAsync(string parentId, string potentialDescendantId, CancellationToken cancellationToken = default)
    {
      var children = await repo.GetChildrenAsync(parentId, cancellationToken);
      foreach (var child in children)
      {
        if (child.Id == potentialDescendantId) return true;
        if (await IsDescendantAsync(child.Id, potentialDescendantId, cancellationToken)) return true;
      }
      return false;
    }
  }
}
