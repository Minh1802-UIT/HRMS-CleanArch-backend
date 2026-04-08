using Employee.Domain.Entities.Attendance;

namespace Employee.Domain.Interfaces.Repositories
{
    public interface IFaceEmbeddingRepository : IBaseRepository<FaceEmbedding>
    {
        /// <summary>Get the approved embedding for an employee (only one allowed).</summary>
        Task<FaceEmbedding?> GetApprovedByEmployeeAsync(string employeeId, CancellationToken ct = default);

        /// <summary>Get latest registration (any status) for an employee.</summary>
        Task<FaceEmbedding?> GetLatestByEmployeeAsync(string employeeId, CancellationToken ct = default);

        /// <summary>Get all pending registrations (for HR/Admin review).</summary>
        Task<List<FaceEmbedding>> GetAllPendingAsync(CancellationToken ct = default);

        /// <summary>Get all registrations across all employees.</summary>
        Task<List<FaceEmbedding>> GetAllAsync(CancellationToken ct = default);
    }
}
