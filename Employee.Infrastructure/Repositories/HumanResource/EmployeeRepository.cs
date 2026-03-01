using Employee.Application.Common.Dtos;
using Employee.Application.Common.Interfaces;
using Employee.Domain.Common.Models;
using Employee.Domain.Entities.HumanResource;
using Employee.Domain.Enums;
using Employee.Domain.Interfaces.Repositories;
using Employee.Infrastructure.Persistence;
using Employee.Infrastructure.Repositories.Common;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Employee.Infrastructure.Repositories.HumanResource
{
    public class EmployeeRepository : BaseRepository<EmployeeEntity>, IEmployeeRepository, IEmployeeQueryRepository
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

            // OPT-6: Use MongoDB $text search to leverage idx_employees_text index
            // instead of $regex which causes a full collection scan.
            if (!string.IsNullOrEmpty(pagination.SearchTerm))
            {
                var textFilter = Builders<EmployeeEntity>.Filter.Text(pagination.SearchTerm);
                filter = Builders<EmployeeEntity>.Filter.And(filter, textFilter);
            }

            // Optional Department Filter
            if (!string.IsNullOrEmpty(pagination.DepartmentId))
            {
                filter = Builders<EmployeeEntity>.Filter.And(filter,
                    Builders<EmployeeEntity>.Filter.Eq("JobDetails.DepartmentId", pagination.DepartmentId));
            }

            // Optional Position Filter
            if (!string.IsNullOrEmpty(pagination.PositionId))
            {
                filter = Builders<EmployeeEntity>.Filter.And(filter,
                    Builders<EmployeeEntity>.Filter.Eq("JobDetails.PositionId", pagination.PositionId));
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
            var pageSize = pagination.PageSize.GetValueOrDefault(20);

            var entities = await _collection
              .Find(filter)
              .Sort(sort)
              .Project<BsonDocument>(projection)
              .Skip((pageNumber - 1) * pageSize)
              .Limit(pageSize)
              .ToListAsync(cancellationToken);

            var summaries = entities.Select(e =>
            {
                var jobDetails = e.TryGetElement("JobDetails", out var jdElem) ? jdElem.Value.AsBsonDocument : null;
                var statusStr = jobDetails != null && jobDetails.TryGetElement("Status", out var stElem)
            ? stElem.Value.ToString() ?? string.Empty : string.Empty;
                return new EmployeeListSummary
                {
                    Id = e.GetValue("_id", BsonNull.Value).ToString()!,
                    EmployeeCode = e.GetValue("EmployeeCode", new BsonString(string.Empty)).AsString,
                    FullName = e.GetValue("FullName", new BsonString(string.Empty)).AsString,
                    AvatarUrl = e.TryGetElement("AvatarUrl", out var avElem) && avElem.Value != BsonNull.Value
                             ? avElem.Value.AsString : null,
                    DepartmentId = jobDetails != null && jobDetails.TryGetElement("DepartmentId", out var dElem)
                             ? dElem.Value.ToString()! : string.Empty,
                    PositionId = jobDetails != null && jobDetails.TryGetElement("PositionId", out var pElem)
                             ? pElem.Value.ToString()! : string.Empty,
                    Status = statusStr
                };
            }).ToList();

            return new PagedResult<EmployeeListSummary>
            {
                Items = summaries,
                TotalCount = (int)totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
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
            // OPT-6: Use $text search instead of $regex to leverage idx_employees_text index
            if (!string.IsNullOrEmpty(keyword))
            {
                var textFilter = Builders<EmployeeEntity>.Filter.Text(keyword);
                filter = Builders<EmployeeEntity>.Filter.And(filter, textFilter);
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
            var results = await _collection.Find(filter).Project<BsonDocument>(projection).ToListAsync(cancellationToken);
            return results.Select(e => e.GetValue("_id").ToString()!).ToList();
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

        public async Task<Dictionary<string, int>> GetDepartmentDistributionAsync(CancellationToken cancellationToken = default)
        {
            var pipeline = new[]
            {
        // Match only active, non-deleted employees with JobDetails
        new BsonDocument("$match", new BsonDocument
        {
          { "IsDeleted", new BsonDocument("$ne", true) },
          { "JobDetails", new BsonDocument("$ne", BsonNull.Value) },
          { "JobDetails.Status", new BsonDocument("$in", new BsonArray { "Active", "Probation" }) }
        }),
        // Group by DepartmentId, count
        new BsonDocument("$group", new BsonDocument
        {
          { "_id", "$JobDetails.DepartmentId" },
          { "count", new BsonDocument("$sum", 1) }
        })
      };

            var results = await _collection.Aggregate<BsonDocument>(pipeline, cancellationToken: cancellationToken).ToListAsync(cancellationToken);
            return results
                .Where(r => r["_id"] != BsonNull.Value && r["_id"].ToString() != "")
                .ToDictionary(r => r["_id"].ToString()!, r => r["count"].AsInt32);
        }
    }
}
