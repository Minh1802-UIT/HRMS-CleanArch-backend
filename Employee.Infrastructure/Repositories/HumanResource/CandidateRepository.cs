using Employee.Domain.Interfaces.Repositories;
using Employee.Domain.Entities.HumanResource;
using Employee.Infrastructure.Persistence;
using Employee.Infrastructure.Repositories.Common;
using MongoDB.Driver;
using MongoDB.Bson;

namespace Employee.Infrastructure.Repositories.HumanResource
{
  public class CandidateRepository : BaseRepository<Candidate>, ICandidateRepository
  {
    public CandidateRepository(IMongoContext context) : base(context, "candidates")
    {
    }

    public async Task<IEnumerable<Candidate>> GetByVacancyIdAsync(string vacancyId, CancellationToken cancellationToken = default)
    {
      var filter = Builders<Candidate>.Filter.And(
          SoftDeleteFilter.GetActiveOnlyFilter<Candidate>(),
          Builders<Candidate>.Filter.Eq(x => x.JobVacancyId, vacancyId));
      return await _collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<string, int>> GetStatusCountsAsync(CancellationToken cancellationToken = default)
    {
      var pipeline = new[]
      {
        new BsonDocument("$match", new BsonDocument("IsDeleted", new BsonDocument("$ne", true))),
        new BsonDocument("$group", new BsonDocument
        {
          { "_id", "$Status" },
          { "count", new BsonDocument("$sum", 1) }
        })
      };

      var results = await _collection.Aggregate<BsonDocument>(pipeline, cancellationToken: cancellationToken).ToListAsync(cancellationToken);
      return results
          .Where(r => r["_id"] != BsonNull.Value)
          .ToDictionary(r => r["_id"].ToString()!, r => r["count"].AsInt32);
    }
  }
}
