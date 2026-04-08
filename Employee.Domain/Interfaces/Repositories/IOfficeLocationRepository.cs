using Employee.Domain.Entities.Attendance;

namespace Employee.Domain.Interfaces.Repositories
{
    public interface IOfficeLocationRepository : IBaseRepository<OfficeLocation>
    {
        Task<List<OfficeLocation>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    }
}
