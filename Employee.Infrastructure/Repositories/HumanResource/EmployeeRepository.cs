using Employee.Application.Common.Interfaces;
using Employee.Application.Common.Interfaces.Organization.IRepository;
using Employee.Infrastructure.Persistence;
using Employee.Application.Common.Models;
using MongoDB.Driver;
using Employee.Domain.Entities.HumanResource;
using Employee.Domain.Enums;
using Employee.Infrastructure.Repositories.Common;

namespace Employee.Infrastructure.Repositories.HumanResource
{
  public class EmployeeRepository : BaseRepository<EmployeeEntity>, IEmployeeRepository
  {
    public EmployeeRepository(IMongoContext context) : base(context, "employees")
    {
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
