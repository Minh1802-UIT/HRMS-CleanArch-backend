using Employee.Application.Common;
using Employee.Application.Common.Exceptions;
using Employee.Application.Common.Interfaces;
using Employee.Domain.Interfaces.Repositories;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Employee.Application.Features.Organization.Commands.UpdatePosition
{
  public class UpdatePositionHandler(
      IPositionRepository repo,
      ICacheService cache,
      IDepartmentRepository deptRepo) : IRequestHandler<UpdatePositionCommand>
  {
    public async Task Handle(UpdatePositionCommand request, CancellationToken cancellationToken)
    {
      var existing = await repo.GetByIdAsync(request.Id, cancellationToken);
      if (existing == null) throw new NotFoundException("Không tìm thấy chức vụ");

      // Cycle Detection
      if (!string.IsNullOrEmpty(request.Dto.ParentId))
      {
        if (request.Dto.ParentId == request.Id) throw new ValidationException("Một chức năng không thể làm cấp trên của chính nó.");

        var isSubordinate = await IsSubordinateAsync(request.Id, request.Dto.ParentId, cancellationToken);
        if (isSubordinate) throw new ValidationException("Không thể đặt cấp trên là một chức vụ thuộc cấp dưới của mình.");

        var parent = await repo.GetByIdAsync(request.Dto.ParentId, cancellationToken);
        if (parent == null) throw new NotFoundException("Không tìm thấy chức vụ cấp trên");
      }

      // 1. Update Department
      if (!string.IsNullOrEmpty(request.Dto.DepartmentId) && request.Dto.DepartmentId != existing.DepartmentId)
      {
        var dept = await deptRepo.GetByIdAsync(request.Dto.DepartmentId, cancellationToken);
        if (dept == null) throw new NotFoundException($"Department with ID {request.Dto.DepartmentId} not found.");
        existing.ChangeDepartment(request.Dto.DepartmentId);
      }

      // 2. Update Salary Range
      if (request.Dto.SalaryRange != null)
      {
        var salaryRange = new Employee.Domain.Entities.ValueObjects.SalaryRange
        {
          Min = request.Dto.SalaryRange.Min,
          Max = request.Dto.SalaryRange.Max,
          Currency = request.Dto.SalaryRange.Currency ?? "VND"
        };
        existing.UpdateInfo(request.Dto.Title ?? existing.Title, salaryRange);
      }
      else
      {
        // Update Title anyway if provided
        if (!string.IsNullOrEmpty(request.Dto.Title) && request.Dto.Title != existing.Title)
        {
          existing.UpdateInfo(request.Dto.Title, existing.SalaryRange);
        }
      }

      // 3. Update Parent
      existing.SetParent(request.Dto.ParentId);

      await repo.UpdateAsync(request.Id, existing, cancellationToken);
      await cache.RemoveAsync(CacheKeys.PositionTree);
    }

    private async Task<bool> IsSubordinateAsync(string parentId, string potentialSubordinateId, CancellationToken cancellationToken = default)
    {
      var children = await repo.GetSubordinatesAsync(parentId, cancellationToken);
      foreach (var child in children)
      {
        if (child.Id == potentialSubordinateId) return true;
        if (await IsSubordinateAsync(child.Id, potentialSubordinateId, cancellationToken)) return true;
      }
      return false;
    }
  }
}
