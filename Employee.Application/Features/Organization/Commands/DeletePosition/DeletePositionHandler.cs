using Employee.Application.Common;
using Employee.Application.Common.Exceptions;
using Employee.Application.Common.Interfaces;
using Employee.Domain.Interfaces.Repositories;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Employee.Application.Features.Organization.Commands.DeletePosition
{
  public class DeletePositionHandler(
      IPositionRepository repo,
      ICacheService cache,
      IEmployeeRepository employeeRepo) : IRequestHandler<DeletePositionCommand>
  {
    public async Task Handle(DeletePositionCommand request, CancellationToken cancellationToken)
    {
      // IMP-1: Check employee references
      var hasEmployees = await employeeRepo.ExistsByPositionIdAsync(request.Id, cancellationToken);
      if (hasEmployees)
        throw new ValidationException("Không thể xóa chức vụ đang có nhân viên.");

      // Check child positions
      var allPositions = await repo.GetAllActiveAsync(cancellationToken);
      if (allPositions.Any(p => p.ParentId == request.Id))
        throw new ValidationException("Không thể xóa chức vụ đang có chức vụ con.");

      await repo.DeleteAsync(request.Id, cancellationToken);
      await cache.RemoveAsync(CacheKeys.PositionTree);
    }
  }
}
