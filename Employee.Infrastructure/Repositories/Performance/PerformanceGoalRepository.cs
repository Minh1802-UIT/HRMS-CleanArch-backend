using System;
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
  public class PerformanceGoalRepository : BaseRepository<PerformanceGoal>, IPerformanceGoalRepository
  {
    public PerformanceGoalRepository(IMongoContext context) : base(context, "performance_goals")
    {
    }

    public async Task<IEnumerable<PerformanceGoal>> GetByEmployeeIdAsync(string employeeId, CancellationToken cancellationToken = default)
    {
      var filter = Builders<PerformanceGoal>.Filter.And(
          SoftDeleteFilter.GetActiveOnlyFilter<PerformanceGoal>(),
          Builders<PerformanceGoal>.Filter.Eq(x => x.EmployeeId, employeeId));
      return await _collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<long> CountAllAsync(CancellationToken cancellationToken = default)
    {
      var filter = SoftDeleteFilter.GetActiveOnlyFilter<PerformanceGoal>();
      return await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
    }

    public async Task<long> CountByStatusAsync(PerformanceGoalStatus status, CancellationToken cancellationToken = default)
    {
      var filter = Builders<PerformanceGoal>.Filter.And(
          SoftDeleteFilter.GetActiveOnlyFilter<PerformanceGoal>(),
          Builders<PerformanceGoal>.Filter.Eq(x => x.Status, status));
      return await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
    }

    public async Task<double> GetAverageProgressAsync(PerformanceGoalStatus status, CancellationToken cancellationToken = default)
    {
      var filter = Builders<PerformanceGoal>.Filter.And(
          SoftDeleteFilter.GetActiveOnlyFilter<PerformanceGoal>(),
          Builders<PerformanceGoal>.Filter.Eq(x => x.Status, status));

      var result = await _collection.Aggregate()
          .Match(filter)
          .Group(_ => 1, g => new { Avg = g.Average(x => x.Progress) })
          .FirstOrDefaultAsync(cancellationToken);

      return result?.Avg ?? 0;
    }

    public async Task<List<MonthlyGoalStatsAggregate>> GetMonthlyStatsAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
      var baseFilter = Builders<PerformanceGoal>.Filter.And(
          SoftDeleteFilter.GetActiveOnlyFilter<PerformanceGoal>(),
          Builders<PerformanceGoal>.Filter.Or(
              Builders<PerformanceGoal>.Filter.Gte(x => x.CreatedAt, fromUtc),
              Builders<PerformanceGoal>.Filter.Gte(x => x.TargetDate, fromUtc),
              Builders<PerformanceGoal>.Filter.Gte("UpdatedAt", fromUtc)));

      var serializer = BsonSerializer.SerializerRegistry.GetSerializer<PerformanceGoal>();
      var registry = BsonSerializer.SerializerRegistry;

      BsonDocument BuildGroup(string fieldName)
      {
        return new BsonDocument("$group", new BsonDocument
        {
          { "_id", new BsonDocument
            {
              { "year", new BsonDocument("$year", "$" + fieldName) },
              { "month", new BsonDocument("$month", "$" + fieldName) }
            }
          },
          { "count", new BsonDocument("$sum", 1) }
        });
      }

      var matchStage = new BsonDocument(
        "$match",
        baseFilter.Render(new RenderArgs<PerformanceGoal>(serializer, registry)));

      var createdPipeline = new BsonArray
      {
        new BsonDocument("$match", new BsonDocument("CreatedAt", new BsonDocument
        {
          { "$gte", fromUtc },
          { "$lt", toUtc }
        })),
        BuildGroup("CreatedAt")
      };

      var completedPipeline = new BsonArray
      {
        new BsonDocument("$match", new BsonDocument
        {
          { "Status", PerformanceGoalStatus.Completed },
          { "UpdatedAt", new BsonDocument
            {
              { "$gte", fromUtc },
              { "$lt", toUtc }
            }
          }
        }),
        BuildGroup("UpdatedAt")
      };

      var overduePipeline = new BsonArray
      {
        new BsonDocument("$match", new BsonDocument
        {
          { "Status", PerformanceGoalStatus.Overdue },
          { "TargetDate", new BsonDocument
            {
              { "$gte", fromUtc },
              { "$lt", toUtc }
            }
          }
        }),
        BuildGroup("TargetDate")
      };

      var facetStage = new BsonDocument("$facet", new BsonDocument
      {
        { "created", createdPipeline },
        { "completed", completedPipeline },
        { "overdue", overduePipeline }
      });

      var result = await _collection.Aggregate<BsonDocument>(
          new[] { matchStage, facetStage })
        .FirstOrDefaultAsync(cancellationToken);

      if (result == null)
      {
        return new List<MonthlyGoalStatsAggregate>();
      }

      static Dictionary<(int Year, int Month), int> ReadCounts(BsonArray array)
      {
        var dict = new Dictionary<(int Year, int Month), int>();
        foreach (var entry in array.OfType<BsonDocument>())
        {
          if (!entry.TryGetValue("_id", out var idValue) || idValue.IsBsonNull)
          {
            continue;
          }

          var idDoc = idValue.AsBsonDocument;
          var year = idDoc.GetValue("year", 0).ToInt32();
          var month = idDoc.GetValue("month", 0).ToInt32();
          var count = entry.GetValue("count", 0).ToInt32();
          dict[(year, month)] = count;
        }
        return dict;
      }

      var createdCounts = ReadCounts(result.GetValue("created", new BsonArray()).AsBsonArray);
      var completedCounts = ReadCounts(result.GetValue("completed", new BsonArray()).AsBsonArray);
      var overdueCounts = ReadCounts(result.GetValue("overdue", new BsonArray()).AsBsonArray);

      var keys = createdCounts.Keys
        .Union(completedCounts.Keys)
        .Union(overdueCounts.Keys)
        .ToList();

      var stats = new List<MonthlyGoalStatsAggregate>();
      foreach (var key in keys)
      {
        createdCounts.TryGetValue(key, out var created);
        completedCounts.TryGetValue(key, out var completed);
        overdueCounts.TryGetValue(key, out var overdue);

        stats.Add(new MonthlyGoalStatsAggregate
        {
          Year = key.Year,
          Month = key.Month,
          Created = created,
          Completed = completed,
          Overdue = overdue
        });
      }

      return stats;
    }

    public async Task<long> MarkOverdueAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
      var cutoff = utcNow.Date;
      var filter = Builders<PerformanceGoal>.Filter.And(
          SoftDeleteFilter.GetActiveOnlyFilter<PerformanceGoal>(),
          Builders<PerformanceGoal>.Filter.Eq(x => x.Status, PerformanceGoalStatus.InProgress),
          Builders<PerformanceGoal>.Filter.Lt(x => x.TargetDate, cutoff));

      var update = Builders<PerformanceGoal>.Update
          .Set(x => x.Status, PerformanceGoalStatus.Overdue)
          .Set(x => x.UpdatedAt, utcNow);

      UpdateResult result;
      if (_context.Session != null)
      {
        result = await _collection.UpdateManyAsync(_context.Session, filter, update, cancellationToken: cancellationToken);
      }
      else
      {
        result = await _collection.UpdateManyAsync(filter, update, cancellationToken: cancellationToken);
      }

      return result.ModifiedCount;
    }
  }
}
