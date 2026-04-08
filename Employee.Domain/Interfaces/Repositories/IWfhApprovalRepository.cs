using Employee.Domain.Entities.Attendance;

namespace Employee.Domain.Interfaces.Repositories
{
    public interface IWfhApprovalRepository : IBaseRepository<WfhApproval>
    {
        Task<WfhApproval?> GetActiveApprovalAsync(string employeeId, DateTime date, CancellationToken cancellationToken = default);
        Task<List<WfhApproval>> GetByEmployeeAsync(string employeeId, CancellationToken cancellationToken = default);
        Task<List<WfhApproval>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    }
}
