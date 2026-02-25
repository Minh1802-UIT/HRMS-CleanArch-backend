using System.Threading;
using Employee.Domain.Entities.Organization;
using Employee.Application.Common.Models;
using Employee.Application.Common.Interfaces.Common;

namespace Employee.Application.Common.Interfaces.Organization.IRepository;

public interface IPositionRepository : IBaseRepository<Position>
{
  // New: For optimized joins
  Task<Dictionary<string, string>> GetNamesByIdsAsync(List<string> ids, CancellationToken cancellationToken = default);

  // Hierarchy
  Task<List<Position>> GetSubordinatesAsync(string parentId, CancellationToken cancellationToken = default);
  Task<List<Position>> GetAllActiveAsync(CancellationToken cancellationToken = default);
}
