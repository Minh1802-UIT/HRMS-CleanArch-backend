using System.Collections.Generic;
using System.Linq;
using Employee.Domain.Common.Models;
using Employee.Domain.Entities.Performance;
using Employee.Domain.Enums;
using Employee.Domain.Interfaces.Repositories;
using Employee.Infrastructure.Persistence;
using Employee.Infrastructure.Repositories.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Employee.Infrastructure.Repositories.Performance
{
  public class PerformanceReviewRepository : BaseRepository<PerformanceReview>, IPerformanceReviewRepository
  {
    public PerformanceReviewRepository(IMongoContext context) : base(context, "performance_reviews")
    {
    }

    public async Task<IEnumerable<PerformanceReview>> GetByEmployeeIdAsync(string employeeId, CancellationToken cancellationToken = default)
    {
      var filter = Builders<PerformanceReview>.Filter.And(
          SoftDeleteFilter.GetActiveOnlyFilter<PerformanceReview>(),
          Builders<PerformanceReview>.Filter.Eq(x => x.EmployeeId, employeeId));
      return await _collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<PerformanceReviewStats> GetCompletedStatsAsync(CancellationToken cancellationToken = default)
    {
      var stats = new PerformanceReviewStats();

      var filter = Builders<PerformanceReview>.Filter.And(
          SoftDeleteFilter.GetActiveOnlyFilter<PerformanceReview>(),
          Builders<PerformanceReview>.Filter.In(x => x.Status,
              new[] { PerformanceReviewStatus.Completed, PerformanceReviewStatus.Acknowledged }));

      var serializer = BsonSerializer.SerializerRegistry.GetSerializer<PerformanceReview>();
      var registry = BsonSerializer.SerializerRegistry;

      var matchStage = new BsonDocument(
        "$match",
        filter.Render(new RenderArgs<PerformanceReview>(serializer, registry)));
      var summaryStage = new BsonDocument("$group", new BsonDocument
      {
        { "_id", 1 },
        { "avgScore", new BsonDocument("$avg", "$OverallScore") },
        { "total", new BsonDocument("$sum", 1) }
      });

      var distributionStage = new BsonDocument("$bucket", new BsonDocument
      {
        { "groupBy", "$OverallScore" },
        { "boundaries", new BsonArray { 0, 21, 41, 61, 81, 101 } },
        { "default", -1 },
        { "output", new BsonDocument("count", new BsonDocument("$sum", 1)) }
      });

      var facetStage = new BsonDocument("$facet", new BsonDocument
      {
        { "summary", new BsonArray { summaryStage } },
        { "distribution", new BsonArray { distributionStage } }
      });

      var result = await _collection
        .Aggregate<BsonDocument>(new[] { matchStage, facetStage })
        .FirstOrDefaultAsync(cancellationToken);

      if (result == null)
      {
        return stats;
      }

      var summaryArray = result.GetValue("summary", new BsonArray()).AsBsonArray;
      if (summaryArray.Count > 0)
      {
        var summary = summaryArray[0].AsBsonDocument;
        stats.AverageScore = summary.GetValue("avgScore", 0).ToDouble();
        stats.TotalReviews = summary.GetValue("total", 0).ToInt32();
      }

      var distributionArray = result.GetValue("distribution", new BsonArray()).AsBsonArray;
      var distribution = new List<int> { 0, 0, 0, 0, 0 };
      var bucketIndex = new Dictionary<int, int>
      {
        { 0, 0 },
        { 21, 1 },
        { 41, 2 },
        { 61, 3 },
        { 81, 4 }
      };

      static int ToInt(BsonValue value)
      {
        if (value.IsInt32) return value.AsInt32;
        if (value.IsInt64) return (int)value.AsInt64;
        if (value.IsDouble) return (int)value.AsDouble;
        if (value.IsString && int.TryParse(value.AsString, out var parsed)) return parsed;
        return 0;
      }

      foreach (var entry in distributionArray.OfType<BsonDocument>())
      {
        if (!entry.TryGetValue("_id", out var idValue))
        {
          continue;
        }

        var bucketKey = ToInt(idValue);
        if (!bucketIndex.TryGetValue(bucketKey, out var index))
        {
          continue;
        }

        distribution[index] = entry.GetValue("count", 0).ToInt32();
      }

      stats.ScoreDistribution = distribution;
      return stats;
    }

    public async Task<List<EmployeeScoreAggregate>> GetAtRiskEmployeesAsync(int top, CancellationToken cancellationToken = default)
    {
      var filter = Builders<PerformanceReview>.Filter.And(
          SoftDeleteFilter.GetActiveOnlyFilter<PerformanceReview>(),
          Builders<PerformanceReview>.Filter.In(x => x.Status,
              new[] { PerformanceReviewStatus.Completed, PerformanceReviewStatus.Acknowledged }));

      var serializer = BsonSerializer.SerializerRegistry.GetSerializer<PerformanceReview>();
      var registry = BsonSerializer.SerializerRegistry;

      var matchStage = new BsonDocument(
        "$match",
        filter.Render(new RenderArgs<PerformanceReview>(serializer, registry)));
      var groupStage = new BsonDocument("$group", new BsonDocument
      {
        { "_id", "$EmployeeId" },
        { "avgScore", new BsonDocument("$avg", "$OverallScore") },
        { "reviewCount", new BsonDocument("$sum", 1) }
      });
      var sortStage = new BsonDocument("$sort", new BsonDocument("avgScore", 1));
      var limitStage = new BsonDocument("$limit", top);

      var results = await _collection.Aggregate<BsonDocument>(
          new[] { matchStage, groupStage, sortStage, limitStage })
        .ToListAsync(cancellationToken);

      return results.Select(doc =>
      {
        var idValue = doc.GetValue("_id", BsonNull.Value);
        var id = idValue.IsBsonNull ? string.Empty : idValue.ToString() ?? string.Empty;

        return new EmployeeScoreAggregate
        {
          EmployeeId = id,
          AverageScore = doc.GetValue("avgScore", 0).ToDouble(),
          ReviewCount = doc.GetValue("reviewCount", 0).ToInt32()
        };
      }).ToList();
    }
  }
}
