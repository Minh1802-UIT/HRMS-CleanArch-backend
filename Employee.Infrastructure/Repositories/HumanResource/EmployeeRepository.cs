using Employee.Application.Common.Interfaces;
using Employee.Application.Common.Interfaces.Organization.IRepository;
using Employee.Infrastructure.Persistence;
using Employee.Application.Common.Models;
using MongoDB.Driver;
using Employee.Domain.Entities.HumanResource;
using Employee.Domain.Enums;
using Employee.Infrastructure.Repositories.Common;
using Employee.Application.Features.HumanResource.Dtos;
using MongoDB.Bson;

namespace Employee.Infrastructure.Repositories.HumanResource
{
  public class EmployeeRepository : BaseRepository<EmployeeEntity>, IEmployeeRepository
  {
    public EmployeeRepository(IMongoContext context) : base(context, "employees")
    {
    }

    // ------------------------------------------------------------------ //
    // Projection-only list query — transfers only the fields rendered on  //
    // the employee list page (~500 bytes vs ~5 KB per document).          //
    // ------------------------------------------------------------------ //
    public async Task<PagedResult<EmployeeListSummary>> GetPagedListAsync(
        PaginationParams pagination,
        CancellationToken cancellationToken = default)
    {
      var filter = SoftDeleteFilter.GetActiveOnlyFilter<EmployeeEntity>();

      // Optional keyword search across FullName / EmployeeCode
      if (!string.IsNullOrEmpty(pagination.SearchTerm))
      {
        var term = pagination.SearchTerm;
        filter &= Builders<EmployeeEntity>.Filter.Or(
          Builders<EmployeeEntity>.Filter.Regex(x => x.FullName, new BsonRegularExpression(term, "i")),
          Builders<EmployeeEntity>.Filter.Regex(x => x.EmployeeCode, new BsonRegularExpression(term, "i"))
        );
      }

      var totalCount = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

      // Project only the fields the list page needs; omit PersonalInfo + BankDetails
      var projection = Builders<EmployeeEntity>.Projection
        .Include(x => x.Id)
        .Include(x => x.EmployeeCode)
        .Include(x => x.FullName)
        .Include(x => x.AvatarUrl)
        .Include("JobDetails.DepartmentId")
        .Include("JobDetails.PositionId")
        .Include("JobDetails.Status");

      // Sort — default FullName ASC; honour explicit SortBy when present
      SortDefinition<EmployeeEntity> sort;
      if (!string.IsNullOrEmpty(pagination.SortBy))
      {
        if (!System.Text.RegularExpressions.Regex.IsMatch(
                pagination.SortBy, @"^[a-zA-Z][a-zA-Z0-9_.]*$"))
          throw new ArgumentException($"SortBy value '{pagination.SortBy}' is not valid.", nameof(pagination));

        sort = pagination.IsDescending.GetValueOrDefault()
          ? Builders<EmployeeEntity>.Sort.Descending(pagination.SortBy)
          : Builders<EmployeeEntity>.Sort.Ascending(pagination.SortBy);
      }
      else
      {
        sort = Builders<EmployeeEntity>.Sort.Ascending(x => x.FullName);
      }

      var pageNumber = pagination.PageNumber.GetValueOrDefault(1);
      var pageSize   = pagination.PageSize.GetValueOrDefault(20);

      var entities = await _collection
        .Find(filter)
        .Sort(sort)
        .Project<EmployeeEntity>(projection)
        .Skip((pageNumber - 1) * pageSize)
        .Limit(pageSize)
        .ToListAsync(cancellationToken);

      var summaries = entities.Select(e => new EmployeeListSummary
      {
        Id           = e.Id,
        EmployeeCode = e.EmployeeCode,
        FullName     = e.FullName,
        AvatarUrl    = e.AvatarUrl,
        DepartmentId = e.JobDetails?.DepartmentId ?? string.Empty,
        PositionId   = e.JobDetails?.PositionId   ?? string.Empty,
        Status       = e.JobDetails?.Status.ToString() ?? string.Empty
      }).ToList();

      return new PagedResult<EmployeeListSummary>
      {
        Items      = summaries,
        TotalCount = (int)totalCount,
        PageNumber = pageNumber,
        PageSize   = pageSize
      };
    }

    public async Task<List<EmployeeEntity>> GetAllIncludingDeletedAsync(CancellationToken cancellationToken = default) =>
        await _collection.Find(_ => true).ToListAsync(cancellationToken);

    public async Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        await _collection.Find(Builders<EmployeeEntity>.Filter.And(
          Builders<EmployeeEntity>.Filter.Eq(x => x.EmployeeCode, code),
          SoftDeleteFilter.GetActiveOnlyFilter<EmployeeEntity>()
        )).AnyAsync(cancellationToken);

    public async Task<List<LookupDto>> GetLookupAsync(string? keyword = null, int limit = 20, CancellationToken cancellationToken = default)
    {
      var filter = SoftDeleteFilter.GetActiveOnlyFilter<EmployeeEntity>();
      if (!string.IsNullOrEmpty(keyword))
      {
        var keywordFilter = Builders<EmployeeEntity>.Filter.Or(
          Builders<EmployeeEntity>.Filter.Regex(x => x.FullName, new MongoDB.Bson.BsonRegularExpression(keyword, "i")),
          Builders<EmployeeEntity>.Filter.Regex(x => x.EmployeeCode, new MongoDB.Bson.BsonRegularExpression(keyword, "i"))
        );
        filter = Builders<EmployeeEntity>.Filter.And(filter, keywordFilter);
      }

      return await _collection
        .Find(filter)
        .Project(x => new LookupDto
        {
          Id = x.Id,
          Label = x.FullName,
          SecondaryLabel = x.EmployeeCode
        })
        .Limit(limit)
        .ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<string, (string Name, string Code)>> GetNamesByIdsAsync(List<string> ids, CancellationToken cancellationToken = default)
    {
      var validIds = ids.Where(id => MongoDB.Bson.ObjectId.TryParse(id, out _)).ToList();
      var filter = Builders<EmployeeEntity>.Filter.And(
        Builders<EmployeeEntity>.Filter.In(x => x.Id, validIds),
        SoftDeleteFilter.GetActiveOnlyFilter<EmployeeEntity>()
      );
      var results = await _collection
        .Find(filter)
        .Project(x => new { x.Id, x.FullName, x.EmployeeCode })
        .ToListAsync(cancellationToken);

      return results.ToDictionary(x => x.Id, x => (x.FullName, x.EmployeeCode));
    }

    public async Task<List<string>> GetActiveEmployeeIdsAsync(CancellationToken cancellationToken = default)
    {
      var filter = SoftDeleteFilter.GetActiveOnlyFilter<EmployeeEntity>();
      var projection = Builders<EmployeeEntity>.Projection.Include(e => e.Id);
      var results = await _collection.Find(filter).Project<EmployeeEntity>(projection).ToListAsync(cancellationToken);
      return results.Select(e => e.Id).ToList();
    }

    public async Task<List<EmployeeEntity>> GetByManagerIdAsync(string managerId, CancellationToken cancellationToken = default)
    {
      var filter = Builders<EmployeeEntity>.Filter.And(
          SoftDeleteFilter.GetActiveOnlyFilter<EmployeeEntity>(),
          Builders<EmployeeEntity>.Filter.Eq("JobDetails.ManagerId", managerId)
      );
      return await _collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<long> CountActiveAsync(CancellationToken cancellationToken = default) =>
        await _collection.CountDocumentsAsync(Builders<EmployeeEntity>.Filter.And(
          SoftDeleteFilter.GetActiveOnlyFilter<EmployeeEntity>(),
          Builders<EmployeeEntity>.Filter.Ne(x => x.JobDetails, null),
          Builders<EmployeeEntity>.Filter.In("JobDetails.Status", new[] { EmployeeStatus.Active, EmployeeStatus.Probation })
        ), cancellationToken: cancellationToken);

    public async Task<List<EmployeeEntity>> GetRecentHiresAsync(int count, CancellationToken cancellationToken = default)
    {
      var filter = SoftDeleteFilter.GetActiveOnlyFilter<EmployeeEntity>();
      return await _collection.Find(filter)
          .SortByDescending(x => x.JobDetails.JoinDate)
          .Limit(count)
          .ToListAsync(cancellationToken);
    }

    public async Task<List<EmployeeEntity>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
      return await _collection.Find(Builders<EmployeeEntity>.Filter.And(
        SoftDeleteFilter.GetActiveOnlyFilter<EmployeeEntity>(),
        Builders<EmployeeEntity>.Filter.Ne(x => x.JobDetails, null),
        Builders<EmployeeEntity>.Filter.In("JobDetails.Status", new[] { EmployeeStatus.Active, EmployeeStatus.Probation })
      )).ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByDepartmentIdAsync(string departmentId, CancellationToken cancellationToken = default) =>
        await _collection.Find(Builders<EmployeeEntity>.Filter.And(
          SoftDeleteFilter.GetActiveOnlyFilter<EmployeeEntity>(),
          Builders<EmployeeEntity>.Filter.Ne(x => x.JobDetails, null),
          Builders<EmployeeEntity>.Filter.Eq("JobDetails.DepartmentId", departmentId)
        )).AnyAsync(cancellationToken);

    public async Task<bool> ExistsByPositionIdAsync(string positionId, CancellationToken cancellationToken = default) =>
        await _collection.Find(Builders<EmployeeEntity>.Filter.And(
          SoftDeleteFilter.GetActiveOnlyFilter<EmployeeEntity>(),
          Builders<EmployeeEntity>.Filter.Ne(x => x.JobDetails, null),
          Builders<EmployeeEntity>.Filter.Eq("JobDetails.PositionId", positionId)
        )).AnyAsync(cancellationToken);
  }
}
