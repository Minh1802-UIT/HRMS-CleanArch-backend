using Employee.Domain.Entities.Attendance;
using Employee.Domain.Interfaces.Repositories;
using Employee.Infrastructure.Persistence;
using Employee.Infrastructure.Repositories.Common;
using MongoDB.Driver;

namespace Employee.Infrastructure.Repositories.Attendance
{
    public class OfficeLocationRepository : BaseRepository<OfficeLocation>, IOfficeLocationRepository
    {
        public OfficeLocationRepository(IMongoContext context)
            : base(context, "office_locations") { }

        public async Task<List<OfficeLocation>> GetAllActiveAsync(CancellationToken cancellationToken = default)
            => await _collection
                .Find(x => x.IsDeleted != true && x.IsActive)
                .SortBy(x => x.Name)
                .ToListAsync(cancellationToken);
    }
}
