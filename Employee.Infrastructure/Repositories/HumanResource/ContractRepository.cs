using Employee.Application.Common.Interfaces;
using Employee.Infrastructure.Persistence;
using Employee.Application.Common.Interfaces.Organization.IRepository;
using Employee.Application.Common.Models;
using Employee.Domain.Entities.HumanResource;
using MongoDB.Driver;
using Employee.Domain.Enums;
using Employee.Infrastructure.Repositories.Common;

namespace Employee.Infrastructure.Repositories.HumanResource
{
  public class ContractRepository : BaseRepository<ContractEntity>, IContractRepository
  {
    public ContractRepository(IMongoContext context) : base(context, "contracts")
    {
    }

    public async Task<List<ContractEntity>> GetByEmployeeIdAsync(string employeeId, CancellationToken cancellationToken = default) =>
        await _collection.Find(x => x.EmployeeId == employeeId && x.IsDeleted != true).ToListAsync(cancellationToken);

    public async Task<List<ContractSalaryProjection>> GetActiveSalaryInfoAsync(CancellationToken cancellationToken = default)
    {
      var filter = Builders<ContractEntity>.Filter.Eq(c => c.Status, ContractStatus.Active);
      var projection = Builders<ContractEntity>.Projection
        .Include(c => (object)c.EmployeeId)
        .Include(c => (object)c.Status)
        .Include("Salary.BasicSalary")
        .Include("Salary.TransportAllowance")
        .Include("Salary.LunchAllowance");

      var results = await _collection
        .Find(filter)
        .Project<ContractEntity>(projection)
        .ToListAsync(cancellationToken);

      return results.Select(c => new ContractSalaryProjection
      {
        EmployeeId = c.EmployeeId,
        Status = c.Status.ToString(),
        BasicSalary = c.Salary?.BasicSalary ?? 0,
        TransportAllowance = c.Salary?.TransportAllowance ?? 0,
        LunchAllowance = c.Salary?.LunchAllowance ?? 0
      }).ToList();
    }

    public override async Task<PagedResult<ContractEntity>> GetPagedAsync(PaginationParams pagination, CancellationToken cancellationToken = default)
    {
      if (!string.IsNullOrEmpty(pagination.SortBy))
      {
        return await base.GetPagedAsync(pagination, cancellationToken);
      }

      // Default sort by StartDate descending if no sortBy provided
      var filter = Builders<ContractEntity>.Filter.Eq(x => x.IsDeleted, false);
      var totalCount = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

      var items = await _collection.Find(filter)
          .Sort(Builders<ContractEntity>.Sort.Descending(x => x.StartDate))
          .Skip((pagination.PageNumber.GetValueOrDefault(1) - 1) * pagination.PageSize.GetValueOrDefault(20))
          .Limit(pagination.PageSize.GetValueOrDefault(20))
          .ToListAsync(cancellationToken);

      return new PagedResult<ContractEntity>
      {
        Items = items,
        TotalCount = (int)totalCount,
        PageNumber = pagination.PageNumber.GetValueOrDefault(1),
        PageSize = pagination.PageSize.GetValueOrDefault(20)
      };
    }

    public async Task<bool> ExistsOverlapAsync(string employeeId, DateTime startDate, DateTime? endDate, List<string>? excludedIds = null, CancellationToken cancellationToken = default)
    {
      var s2 = startDate;
      var e2 = endDate ?? DateTime.MaxValue;
      var filterBuilder = Builders<ContractEntity>.Filter;

      var filters = new List<FilterDefinition<ContractEntity>>
      {
          filterBuilder.Eq(x => x.IsDeleted, false),
          filterBuilder.Eq(x => x.EmployeeId, employeeId),
          filterBuilder.Ne(x => x.Status, ContractStatus.Terminated),
          filterBuilder.Lte(x => x.StartDate, e2),
          filterBuilder.Or(
              filterBuilder.Eq(x => x.EndDate, null),
              filterBuilder.Gte(x => x.EndDate, s2)
          )
      };

      if (excludedIds != null && excludedIds.Any())
      {
        filters.Add(filterBuilder.Nin(x => x.Id, excludedIds));
      }

      var filter = filterBuilder.And(filters);
      return await _collection.Find(filter).AnyAsync(cancellationToken);
    }

    public async Task<List<ContractEntity>> GetExpiredActiveContractsAsync(DateTime currentDate, CancellationToken cancellationToken = default)
    {
      var filter = Builders<ContractEntity>.Filter.And(
          Builders<ContractEntity>.Filter.Eq(x => x.IsDeleted, false),
          Builders<ContractEntity>.Filter.Eq(x => x.Status, ContractStatus.Active),
          Builders<ContractEntity>.Filter.Lt(x => x.EndDate, currentDate)
      );

      return await _collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task DeleteByEmployeeIdAsync(string employeeId, CancellationToken cancellationToken = default)
    {
      var update = Builders<ContractEntity>.Update
          .Set(x => x.IsDeleted, true)
          .Set(x => x.UpdatedAt, DateTime.UtcNow);
      await _collection.UpdateManyAsync(x => x.EmployeeId == employeeId && x.IsDeleted != true, update, cancellationToken: cancellationToken);
    }
  }
}
