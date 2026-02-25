using Employee.Application.Common.Interfaces;
using Employee.Infrastructure.Persistence;
using Employee.Application.Common.Interfaces.Organization.IRepository;
using Employee.Domain.Entities.Leave;
using MongoDB.Driver;
using Employee.Application.Common.Models;
using Employee.Infrastructure.Repositories.Common;

namespace Employee.Infrastructure.Repositories.Leave
{
  public class LeaveAllocationRepository : BaseRepository<LeaveAllocation>, ILeaveAllocationRepository
  {
    public LeaveAllocationRepository(IMongoContext context) : base(context, "leave_allocations")
    {
    }

    public async Task<LeaveAllocation?> GetByEmployeeAndTypeAsync(string employeeId, string leaveTypeId, string year, CancellationToken cancellationToken = default) =>
        await _collection.Find(x => x.EmployeeId == employeeId && x.LeaveTypeId == leaveTypeId && x.Year == year && x.IsDeleted != true).FirstOrDefaultAsync(cancellationToken);

    public async Task<IEnumerable<LeaveAllocation>> GetByEmployeeAsync(string employeeId, string year, CancellationToken cancellationToken = default) =>
        await _collection.Find(x => x.EmployeeId == employeeId && x.Year == year && x.IsDeleted != true).ToListAsync(cancellationToken);

    public async Task<IEnumerable<LeaveAllocation>> GetByEmployeeIdAsync(string employeeId, CancellationToken cancellationToken = default) =>
        await _collection.Find(x => x.EmployeeId == employeeId && x.IsDeleted != true).ToListAsync(cancellationToken);

    public async Task<PagedResult<LeaveAllocation>> GetPagedAsync(PaginationParams pagination, List<string>? employeeIds = null, CancellationToken cancellationToken = default)
    {
      var builder = Builders<LeaveAllocation>.Filter;
      var filter = builder.Eq(x => x.IsDeleted, false);

      if (employeeIds != null && employeeIds.Any())
      {
        filter &= builder.In(x => x.EmployeeId, employeeIds);
      }

      var totalCount = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
      var query = _collection.Find(filter);

      if (!string.IsNullOrEmpty(pagination.SortBy))
      {
        var sortDefinition = pagination.IsDescending.GetValueOrDefault()
            ? Builders<LeaveAllocation>.Sort.Descending(pagination.SortBy)
            : Builders<LeaveAllocation>.Sort.Ascending(pagination.SortBy);
        query = query.Sort(sortDefinition);
      }

      var items = await query
          .Skip((pagination.PageNumber.GetValueOrDefault(1) - 1) * pagination.PageSize.GetValueOrDefault(20))
          .Limit(pagination.PageSize.GetValueOrDefault(20))
          .ToListAsync(cancellationToken);

      return new PagedResult<LeaveAllocation>
      {
        Items = items,
        TotalCount = (int)totalCount,
        PageNumber = pagination.PageNumber.GetValueOrDefault(1),
        PageSize = pagination.PageSize.GetValueOrDefault(20)
      };
    }

    public async Task<IEnumerable<LeaveAllocation>> GetByEmployeeIdsAndYearAsync(List<string> employeeIds, string year, CancellationToken cancellationToken = default)
    {
      var filter = Builders<LeaveAllocation>.Filter.In(x => x.EmployeeId, employeeIds)
                 & Builders<LeaveAllocation>.Filter.Eq(x => x.Year, year)
                 & Builders<LeaveAllocation>.Filter.Eq(x => x.IsDeleted, false);

      return await _collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task BulkUpsertAsync(List<LeaveAllocation> allocations, CancellationToken cancellationToken = default)
    {
      if (allocations == null || !allocations.Any()) return;

      var models = allocations.Select(a =>
      {
        var filter = Builders<LeaveAllocation>.Filter.Eq(x => x.Id, a.Id);
        if (string.IsNullOrEmpty(a.Id))
        {
          filter = Builders<LeaveAllocation>.Filter.Eq(x => x.EmployeeId, a.EmployeeId)
                 & Builders<LeaveAllocation>.Filter.Eq(x => x.LeaveTypeId, a.LeaveTypeId)
                 & Builders<LeaveAllocation>.Filter.Eq(x => x.Year, a.Year);
        }

        return new ReplaceOneModel<LeaveAllocation>(filter, a) { IsUpsert = true };
      }).ToList();

      if (_context.Session != null)
        await _collection.BulkWriteAsync(_context.Session, models, cancellationToken: cancellationToken);
      else
        await _collection.BulkWriteAsync(models, cancellationToken: cancellationToken);
    }

    public async Task DeleteByEmployeeIdAsync(string employeeId, CancellationToken cancellationToken = default)
    {
      var update = Builders<LeaveAllocation>.Update
          .Set(x => x.IsDeleted, true)
          .Set(x => x.UpdatedAt, DateTime.UtcNow);
      await _collection.UpdateManyAsync(x => x.EmployeeId == employeeId && x.IsDeleted != true, update, cancellationToken: cancellationToken);
    }
  }
}
