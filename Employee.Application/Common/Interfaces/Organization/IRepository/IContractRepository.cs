using System.Threading;
using Employee.Application.Common.Models;
using Employee.Domain.Entities.HumanResource;
using Employee.Application.Common.Interfaces.Common;

namespace Employee.Application.Common.Interfaces.Organization.IRepository;

public interface IContractRepository : IBaseRepository<ContractEntity>
{
  // Lấy danh sách hợp đồng của 1 nhân viên
  Task<List<ContractEntity>> GetByEmployeeIdAsync(string employeeId, CancellationToken cancellationToken = default);

  Task<List<ContractSalaryProjection>> GetActiveSalaryInfoAsync(CancellationToken cancellationToken = default);

  Task<bool> ExistsOverlapAsync(string employeeId, DateTime startDate, DateTime? endDate, List<string>? excludedIds = null, CancellationToken cancellationToken = default);
  Task<List<ContractEntity>> GetExpiredActiveContractsAsync(DateTime currentDate, CancellationToken cancellationToken = default); // For Background Job
  Task DeleteByEmployeeIdAsync(string employeeId, CancellationToken cancellationToken = default); // IMP-3
}
