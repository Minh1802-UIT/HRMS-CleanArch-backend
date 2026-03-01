using System.Threading;
using Employee.Domain.Entities.Leave;
using Employee.Domain.Common.Models;
using Employee.Domain.Interfaces.Repositories;

namespace Employee.Domain.Interfaces.Repositories
{
  public interface ILeaveTypeRepository : IBaseRepository<LeaveType>
  {
    Task<LeaveType?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
  }
}
