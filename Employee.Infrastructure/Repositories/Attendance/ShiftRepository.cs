using Employee.Application.Common.Interfaces.Organization.IRepository;
using Employee.Domain.Entities.Attendance;
using MongoDB.Driver;
using Employee.Application.Common.Models;
using Employee.Infrastructure.Persistence;
using Employee.Infrastructure.Repositories.Common;

namespace Employee.Infrastructure.Repositories.Attendance
{
  public class ShiftRepository : BaseRepository<Shift>, IShiftRepository
  {
    public ShiftRepository(IMongoContext context) : base(context, "shifts")
    {
    }

    public async Task<Shift?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        await _collection.Find(x => x.Code == code && x.IsDeleted != true).FirstOrDefaultAsync(cancellationToken);

    public Task<Shift?> GetShiftByDateAsync(string employeeId, DateTime date, CancellationToken cancellationToken = default)
    {
      // Legacy implementation - return null as placeholder or implement logic if needed
      return Task.FromResult<Shift?>(null);
    }

    public async Task<List<Shift>> GetAllActiveAsync(CancellationToken cancellationToken = default) =>
        await _collection.Find(x => x.IsDeleted == false && x.IsActive == true).ToListAsync(cancellationToken);
  }
}
