using Employee.Application.Common.Interfaces.Organization.IRepository;
using Employee.Domain.Entities.Leave;
using MongoDB.Driver;
using Employee.Infrastructure.Persistence;
using Employee.Infrastructure.Repositories.Common;

namespace Employee.Infrastructure.Repositories.Leave
{
  public class LeaveTypeRepository : BaseRepository<LeaveType>, ILeaveTypeRepository
  {
    public LeaveTypeRepository(IMongoContext context) : base(context, "leave_types")
    {
    }

    public async Task<LeaveType?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        await _collection.Find(x => x.Code == code && x.IsDeleted != true).FirstOrDefaultAsync(cancellationToken);
  }
}
