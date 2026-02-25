using System.Threading;
using Employee.Domain.Entities.Leave;
using Employee.Application.Common.Models;
using Employee.Application.Common.Interfaces.Common;

namespace Employee.Application.Common.Interfaces.Organization.IRepository
{
  public interface ILeaveTypeRepository : IBaseRepository<LeaveType>
  {
    Task<LeaveType?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
  }
}
