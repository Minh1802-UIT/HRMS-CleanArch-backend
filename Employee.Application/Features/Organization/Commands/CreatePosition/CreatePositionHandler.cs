using Employee.Application.Common.Exceptions;
using Employee.Application.Common.Interfaces;
using Employee.Application.Common.Interfaces.Organization.IRepository;
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
    private const string POSITION_TREE_KEY = "POSITION_TREE";

    public async Task<string> Handle(CreatePositionCommand request, CancellationToken cancellationToken)
    {
      if (!string.IsNullOrEmpty(request.Dto.ParentId))
      {
        var parent = await repo.GetByIdAsync(request.Dto.ParentId, cancellationToken);
        if (parent == null) throw new NotFoundException("Không tìm thấy chức vụ cấp trên");
      }

      // Validate Department
      var dept = await deptRepo.GetByIdAsync(request.Dto.DepartmentId, cancellationToken);
      if (dept == null) throw new NotFoundException($"Department with ID {request.Dto.DepartmentId} not found.");

      var pos = request.Dto.ToEntity();

      await repo.CreateAsync(pos, cancellationToken);
      await cache.RemoveAsync(POSITION_TREE_KEY);
      return pos.Id;
    }
  }
}
