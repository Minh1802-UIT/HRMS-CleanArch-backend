using System.Threading;
using Employee.Application.Common.Models;
using Employee.Domain.Entities.Payroll;
using Employee.Application.Common.Interfaces.Common;
using System.Collections.Generic;

namespace Employee.Application.Common.Interfaces.Organization.IRepository
{
  public interface IPayrollRepository : IBaseRepository<PayrollEntity>
  {
    Task<List<PayrollEntity>> GetByMonthAsync(string month, CancellationToken cancellationToken = default);
    Task<List<PayrollEntity>> GetByMonthsAsync(IEnumerable<string> months, CancellationToken cancellationToken = default); // OPT-2
    Task<PayrollEntity?> GetByEmployeeAndMonthAsync(string employeeId, string month, CancellationToken cancellationToken = default);
    Task<List<PayrollEntity>> GetByEmployeeIdAsync(string employeeId, CancellationToken cancellationToken = default);

    Task<PagedResult<PayrollEntity>> GetByMonthPagedAsync(string month, PaginationParams pagination, CancellationToken cancellationToken = default);
    Task DeleteByEmployeeIdAsync(string employeeId, CancellationToken cancellationToken = default); // IMP-3
  }
}