using Employee.Application.Common.Interfaces;
using Employee.Infrastructure.Persistence;
using Employee.Domain.Interfaces.Repositories;
using Employee.Domain.Common.Models;
using Employee.Domain.Entities.Payroll;
using MongoDB.Driver;
using System.Collections.Generic;
using Employee.Infrastructure.Repositories.Common;

namespace Employee.Infrastructure.Repositories.Payroll
{
  public class PayrollRepository : BaseRepository<PayrollEntity>, IPayrollRepository
  {
    public PayrollRepository(IMongoContext context) : base(context, "payrolls")
    {
    }

    public async Task<PayrollEntity?> GetByEmployeeAndMonthAsync(string employeeId, string month, CancellationToken cancellationToken = default)
    {
      var filter = Builders<PayrollEntity>.Filter.And(
          SoftDeleteFilter.GetActiveOnlyFilter<PayrollEntity>(),
          Builders<PayrollEntity>.Filter.Eq(x => x.EmployeeId, employeeId),
          Builders<PayrollEntity>.Filter.Eq(x => x.Month, month));
      return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<PayrollEntity>> GetByMonthAsync(string month, CancellationToken cancellationToken = default)
    {
      var filter = Builders<PayrollEntity>.Filter.And(
          SoftDeleteFilter.GetActiveOnlyFilter<PayrollEntity>(),
          Builders<PayrollEntity>.Filter.Eq(x => x.Month, month));
      return await _collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<List<PayrollEntity>> GetByMonthsAsync(IEnumerable<string> months, CancellationToken cancellationToken = default)
    {
      var filter = Builders<PayrollEntity>.Filter.And(
          SoftDeleteFilter.GetActiveOnlyFilter<PayrollEntity>(),
          Builders<PayrollEntity>.Filter.In(x => x.Month, months));
      return await _collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<List<PayrollEntity>> GetByEmployeeIdAsync(string employeeId, CancellationToken cancellationToken = default)
    {
      var filter = Builders<PayrollEntity>.Filter.And(
          SoftDeleteFilter.GetActiveOnlyFilter<PayrollEntity>(),
          Builders<PayrollEntity>.Filter.Eq(x => x.EmployeeId, employeeId));
      return await _collection.Find(filter)
                               .SortByDescending(x => x.Month)
                               .ToListAsync(cancellationToken);
    }

    public override async Task<PagedResult<PayrollEntity>> GetPagedAsync(PaginationParams pagination, CancellationToken cancellationToken = default)
    {
      if (!string.IsNullOrEmpty(pagination.SortBy))
      {
        return await base.GetPagedAsync(pagination, cancellationToken);
      }

      var filter = SoftDeleteFilter.GetActiveOnlyFilter<PayrollEntity>();
      var totalCount = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

      var items = await _collection.Find(filter)
        .Sort(Builders<PayrollEntity>.Sort.Descending(x => x.Month))
        .Skip((pagination.PageNumber.GetValueOrDefault(1) - 1) * pagination.PageSize.GetValueOrDefault(20))
        .Limit(pagination.PageSize.GetValueOrDefault(20))
        .ToListAsync(cancellationToken);

      return new PagedResult<PayrollEntity>
      {
        Items = items,
        TotalCount = (int)totalCount,
        PageNumber = pagination.PageNumber.GetValueOrDefault(1),
        PageSize = pagination.PageSize.GetValueOrDefault(20)
      };
    }

    public async Task<PagedResult<PayrollEntity>> GetByMonthPagedAsync(string month, PaginationParams pagination, CancellationToken cancellationToken = default)
    {
      var filter = Builders<PayrollEntity>.Filter.And(
          SoftDeleteFilter.GetActiveOnlyFilter<PayrollEntity>(),
          Builders<PayrollEntity>.Filter.Eq(x => x.Month, month));
      var totalCount = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
      var query = _collection.Find(filter);

      if (!string.IsNullOrEmpty(pagination.SortBy))
      {
        var sortDefinition = pagination.IsDescending.GetValueOrDefault()
          ? Builders<PayrollEntity>.Sort.Descending(pagination.SortBy)
          : Builders<PayrollEntity>.Sort.Ascending(pagination.SortBy);
        query = query.Sort(sortDefinition);
      }

      var items = await query
        .Skip((pagination.PageNumber.GetValueOrDefault(1) - 1) * pagination.PageSize.GetValueOrDefault(20))
        .Limit(pagination.PageSize.GetValueOrDefault(20))
        .ToListAsync(cancellationToken);

      return new PagedResult<PayrollEntity>
      {
        Items = items,
        TotalCount = (int)totalCount,
        PageNumber = pagination.PageNumber.GetValueOrDefault(1),
        PageSize = pagination.PageSize.GetValueOrDefault(20)
      };
    }

    public async Task DeleteByEmployeeIdAsync(string employeeId, CancellationToken cancellationToken = default)
    {
      // Soft-delete: payroll records are financial audit data and must be retained.
      var update = Builders<PayrollEntity>.Update
          .Set(x => x.IsDeleted, true)
          .Set(x => x.UpdatedAt, DateTime.UtcNow);
      await _collection.UpdateManyAsync(
          Builders<PayrollEntity>.Filter.Where(x => x.EmployeeId == employeeId && x.IsDeleted != true),
          update,
          cancellationToken: cancellationToken);
    }

    public async Task<long> ApproveDraftsByMonthAsync(string monthKey, CancellationToken cancellationToken = default)
    {
      var filter = Builders<PayrollEntity>.Filter.And(
          SoftDeleteFilter.GetActiveOnlyFilter<PayrollEntity>(),
          Builders<PayrollEntity>.Filter.Eq(x => x.Month, monthKey),
          Builders<PayrollEntity>.Filter.Eq(x => x.Status, Employee.Domain.Enums.PayrollStatus.Draft)
      );

      var update = Builders<PayrollEntity>.Update
          .Set(x => x.Status, Employee.Domain.Enums.PayrollStatus.Approved)
          .Set(x => x.UpdatedAt, DateTime.UtcNow);

      var result = await _collection.UpdateManyAsync(filter, update, cancellationToken: cancellationToken);
      return result.ModifiedCount;
    }
  }
}
