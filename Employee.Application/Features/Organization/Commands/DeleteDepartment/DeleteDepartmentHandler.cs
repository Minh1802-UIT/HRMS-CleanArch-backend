using Employee.Application.Common;
using Employee.Application.Common.Exceptions;
using Employee.Application.Common.Interfaces;
using Employee.Domain.Interfaces.Repositories;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Employee.Application.Features.Organization.Commands.DeleteDepartment
{
  public class DeleteDepartmentHandler(
      IDepartmentRepository repo,
      ICacheService cache,
      IEmployeeRepository employeeRepo) : IRequestHandler<DeleteDepartmentCommand>
  {
    public async Task Handle(DeleteDepartmentCommand request, CancellationToken cancellationToken)
    {
      // Check employee references
      var hasEmployees = await employeeRepo.ExistsByDepartmentIdAsync(request.Id, cancellationToken);
      if (hasEmployees)
        throw new ValidationException("Cannot delete a department that has active employees.");

      // Check sub-departments
      var children = await repo.GetChildrenAsync(request.Id, cancellationToken);
      if (children.Any())
        throw new ValidationException("Cannot delete a department that has sub-departments.");

      await repo.DeleteAsync(request.Id, cancellationToken);
      await cache.RemoveAsync(CacheKeys.DepartmentTree);
    }
  }
}
