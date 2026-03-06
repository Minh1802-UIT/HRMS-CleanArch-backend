using Employee.Application.Common;
using Employee.Application.Common.Interfaces;
using Employee.Application.Common.Models;
using Employee.Application.Common.Exceptions;
using Employee.Domain.Interfaces.Repositories;
using Employee.Domain.Events;
using MediatR;

namespace Employee.Application.Features.HumanResource.Commands.DeleteEmployee
{
  public class DeleteEmployeeHandler : IRequestHandler<DeleteEmployeeCommand>
  {
    private readonly IEmployeeRepository _repo;
    private readonly IPublisher _publisher;
    private readonly IDepartmentRepository _deptRepo;
    private readonly ICacheService _cache;

    public DeleteEmployeeHandler(
        IEmployeeRepository repo,
        IPublisher publisher,
        IDepartmentRepository deptRepo,
        ICacheService cache)
    {
      _repo = repo;
      _publisher = publisher;
      _deptRepo = deptRepo;
      _cache = cache;
    }

    public async Task Handle(DeleteEmployeeCommand request, CancellationToken cancellationToken)
    {
      var emp = await _repo.GetByIdAsync(request.Id, cancellationToken);
      if (emp == null) throw new NotFoundException($"Employee with ID '{request.Id}' not found.");

      // 1. Block deletion if the employee is currently a department manager
      var isManager = await _deptRepo.ExistsByManagerIdAsync(request.Id, cancellationToken);
      if (isManager)
      {
        throw new ValidationException("Cannot delete this employee because they are the Manager of a department. Please reassign the manager role first.");
      }

      // 2. Delete from database
      await _repo.DeleteAsync(request.Id, cancellationToken);

      // 3. Publish domain event — event handler handles: deactivate auth user, audit log, contracts...
      await _publisher.Publish(new EmployeeDeletedEvent(request.Id, emp.EmployeeCode, emp.FullName), cancellationToken);

      // 3. Invalidate caches
      await _cache.RemoveAsync(CacheKeys.Employee(request.Id));
      await _cache.RemoveAsync(CacheKeys.EmployeeLookup);
    }
  }
}

