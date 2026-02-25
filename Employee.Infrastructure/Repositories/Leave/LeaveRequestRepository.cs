using Employee.Application.Common.Interfaces;
using Employee.Infrastructure.Persistence;
using Employee.Application.Common.Interfaces.Organization.IRepository;
using Employee.Application.Common.Models;
using Employee.Domain.Entities.Leave;
using Employee.Domain.Enums;
using MongoDB.Driver;
using Employee.Infrastructure.Repositories.Common;

namespace Employee.Infrastructure.Repositories.Leave
{
  public class LeaveRequestRepository : BaseRepository<LeaveRequest>, ILeaveRequestRepository
  {
    public LeaveRequestRepository(IMongoContext context) : base(context, "leave_requests")
    {
    }

    public async Task<List<LeaveRequest>> GetByEmployeeIdAsync(string employeeId, CancellationToken cancellationToken = default) =>
        await _collection.Find(x => x.EmployeeId == employeeId && x.IsDeleted != true).ToListAsync(cancellationToken);

    public override async Task<PagedResult<LeaveRequest>> GetPagedAsync(PaginationParams pagination, CancellationToken cancellationToken = default)
    {
      if (!string.IsNullOrEmpty(pagination.SortBy))
      {
        return await base.GetPagedAsync(pagination, cancellationToken);
      }

      var filter = Builders<LeaveRequest>.Filter.Eq(x => x.IsDeleted, false);
      var totalCount = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

      var items = await _collection.Find(filter)
          .Sort(Builders<LeaveRequest>.Sort.Descending(x => x.FromDate))
          .Skip((pagination.PageNumber.GetValueOrDefault(1) - 1) * pagination.PageSize.GetValueOrDefault(20))
          .Limit(pagination.PageSize.GetValueOrDefault(20))
          .ToListAsync(cancellationToken);

      return new PagedResult<LeaveRequest>
      {
        Items = items,
        TotalCount = (int)totalCount,
        PageNumber = pagination.PageNumber.GetValueOrDefault(1),
        PageSize = pagination.PageSize.GetValueOrDefault(20)
      };
    }

    public async Task<bool> ExistsOverlapAsync(string employeeId, DateTime from, DateTime to, string? excludeId = null, CancellationToken cancellationToken = default)
    {
      var filter = Builders<LeaveRequest>.Filter.And(
          Builders<LeaveRequest>.Filter.Eq(x => x.IsDeleted, false),
          Builders<LeaveRequest>.Filter.Eq(x => x.EmployeeId, employeeId),
          Builders<LeaveRequest>.Filter.Ne(x => x.Status, LeaveStatus.Rejected),
          Builders<LeaveRequest>.Filter.Ne(x => x.Status, LeaveStatus.Cancelled),
          Builders<LeaveRequest>.Filter.Lte(x => x.FromDate, to),
          Builders<LeaveRequest>.Filter.Gte(x => x.ToDate, from)
      );

      // When updating, exclude the current request so it doesn't conflict with itself
      if (!string.IsNullOrEmpty(excludeId))
      {
        filter = Builders<LeaveRequest>.Filter.And(
            filter,
            Builders<LeaveRequest>.Filter.Ne(x => x.Id, excludeId)
        );
      }

      return await _collection.Find(filter).AnyAsync(cancellationToken);
    }

    public async Task<long> CountByStatusAsync(LeaveStatus status, CancellationToken cancellationToken = default) =>
        await _collection.CountDocumentsAsync(x => x.IsDeleted == false && x.Status == status, cancellationToken: cancellationToken);

    public async Task<List<LeaveRequest>> GetRecentAsync(int count, CancellationToken cancellationToken = default)
    {
      return await _collection.Find(x => x.IsDeleted == false)
          .SortByDescending(x => x.CreatedAt)
          .Limit(count)
          .ToListAsync(cancellationToken);
    }

    public async Task DeleteByEmployeeIdAsync(string employeeId, CancellationToken cancellationToken = default)
    {
      var update = Builders<LeaveRequest>.Update
          .Set(x => x.IsDeleted, true)
          .Set(x => x.UpdatedAt, DateTime.UtcNow);
      await _collection.UpdateManyAsync(x => x.EmployeeId == employeeId && x.IsDeleted != true, update, cancellationToken: cancellationToken);
    }
  }
}
