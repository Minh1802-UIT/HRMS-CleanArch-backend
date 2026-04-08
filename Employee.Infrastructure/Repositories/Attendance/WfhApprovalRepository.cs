using Employee.Domain.Entities.Attendance;
using Employee.Domain.Interfaces.Repositories;
using Employee.Infrastructure.Persistence;
using Employee.Infrastructure.Repositories.Common;
using MongoDB.Driver;

namespace Employee.Infrastructure.Repositories.Attendance
{
    public class WfhApprovalRepository : BaseRepository<WfhApproval>, IWfhApprovalRepository
    {
        public WfhApprovalRepository(IMongoContext context)
            : base(context, "wfh_approvals") { }

        public async Task<WfhApproval?> GetActiveApprovalAsync(
            string employeeId, DateTime date, CancellationToken cancellationToken = default)
        {
            var dateOnly = date.Date;
            return await _collection
                .Find(x => x.EmployeeId == employeeId
                        && x.IsActive
                        && x.IsDeleted != true
                        && x.FromDate <= dateOnly
                        && x.ToDate >= dateOnly)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<List<WfhApproval>> GetByEmployeeAsync(
            string employeeId, CancellationToken cancellationToken = default)
            => await _collection
                .Find(x => x.EmployeeId == employeeId && x.IsDeleted != true)
                .SortByDescending(x => x.FromDate)
                .ToListAsync(cancellationToken);

        public async Task<List<WfhApproval>> GetAllActiveAsync(CancellationToken cancellationToken = default)
            => await _collection
                .Find(x => x.IsActive && x.IsDeleted != true)
                .SortByDescending(x => x.FromDate)
                .ToListAsync(cancellationToken);
    }
}
