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
    private const string DEPARTMENT_TREE_KEY = "DEPARTMENT_TREE";

    public async Task Handle(DeleteDepartmentCommand request, CancellationToken cancellationToken)
    {
      // Check employee references
      var hasEmployees = await employeeRepo.ExistsByDepartmentIdAsync(request.Id, cancellationToken);
      if (hasEmployees)
        throw new ValidationException("Không thể xóa phòng ban đang có nhân viên.");

      // Check sub-departments
      var children = await repo.GetChildrenAsync(request.Id, cancellationToken);
      if (children.Any())
        throw new ValidationException("Không thể xóa phòng ban đang có phòng ban con.");

      await repo.DeleteAsync(request.Id, cancellationToken);
      await cache.RemoveAsync(DEPARTMENT_TREE_KEY);
    }
  }
}
