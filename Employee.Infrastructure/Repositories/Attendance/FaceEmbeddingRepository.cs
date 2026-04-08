using Employee.Domain.Entities.Attendance;
using Employee.Domain.Interfaces.Repositories;
using Employee.Infrastructure.Persistence;
using Employee.Infrastructure.Repositories.Common;
using MongoDB.Driver;

namespace Employee.Infrastructure.Repositories.Attendance
{
    public class FaceEmbeddingRepository : BaseRepository<FaceEmbedding>, IFaceEmbeddingRepository
    {
        public FaceEmbeddingRepository(IMongoContext context)
            : base(context, "face_embeddings") { }

        public async Task<FaceEmbedding?> GetApprovedByEmployeeAsync(
            string employeeId, CancellationToken ct = default)
        {
            return await _collection
                .Find(x => x.EmployeeId == employeeId
                        && x.Status == FaceRegistrationStatus.Approved
                        && x.IsDeleted != true)
                .SortByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<FaceEmbedding?> GetLatestByEmployeeAsync(
            string employeeId, CancellationToken ct = default)
        {
            return await _collection
                .Find(x => x.EmployeeId == employeeId && x.IsDeleted != true)
                .SortByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<List<FaceEmbedding>> GetAllPendingAsync(CancellationToken ct = default)
        {
            return await _collection
                .Find(x => x.Status == FaceRegistrationStatus.Pending && x.IsDeleted != true)
                .SortByDescending(x => x.CreatedAt)
                .ToListAsync(ct);
        }

        public async Task<List<FaceEmbedding>> GetAllAsync(CancellationToken ct = default)
        {
            return await _collection
                .Find(x => x.IsDeleted != true)
                .SortByDescending(x => x.CreatedAt)
                .ToListAsync(ct);
        }
    }
}
