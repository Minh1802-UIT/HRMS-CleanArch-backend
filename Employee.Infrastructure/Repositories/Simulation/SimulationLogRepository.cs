using Employee.Domain.Interfaces.Repositories;
using Employee.Domain.Entities.Simulation;
using Employee.Infrastructure.Persistence;
using Employee.Infrastructure.Repositories.Common;
using MongoDB.Driver;

namespace Employee.Infrastructure.Repositories.Simulation;

public class SimulationLogRepository : BaseRepository<SimulationLog>, ISimulationLogRepository
{
    public SimulationLogRepository(IMongoContext context) : base(context, "simulation_logs")
    {
    }

    public async Task<List<SimulationLog>> GetByBotIdAsync(string botId, int limit = 100, CancellationToken cancellationToken = default)
    {
        return await _collection.Find(x => x.BotId == botId)
            .SortByDescending(x => x.SimulatedAtUtc)
            .Limit(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<SimulationLog>> GetByDateAsync(DateTime dateUtc, CancellationToken cancellationToken = default)
    {
        var start = dateUtc.Date;
        var end = start.AddDays(1);

        var filter = Builders<SimulationLog>.Filter.And(
            Builders<SimulationLog>.Filter.Gte(x => x.SimulatedDateUtc, start),
            Builders<SimulationLog>.Filter.Lt(x => x.SimulatedDateUtc, end)
        );

        return await _collection.Find(filter)
            .SortByDescending(x => x.SimulatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<SimulationLog>> GetByDateRangeAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        var filter = Builders<SimulationLog>.Filter.And(
            Builders<SimulationLog>.Filter.Gte(x => x.SimulatedDateUtc, fromUtc.Date),
            Builders<SimulationLog>.Filter.Lt(x => x.SimulatedDateUtc, toUtc.Date.AddDays(1))
        );

        return await _collection.Find(filter)
            .SortByDescending(x => x.SimulatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<SimulationActionType, int>> GetActionStatsAsync(int days = 7, CancellationToken cancellationToken = default)
    {
        var from = DateTime.UtcNow.Date.AddDays(-days);
        var filter = Builders<SimulationLog>.Filter.Gte(x => x.SimulatedDateUtc, from);
        var logs = await _collection.Find(filter).ToListAsync(cancellationToken);

        var result = new Dictionary<SimulationActionType, int>();
        foreach (var group in logs.GroupBy(x => x.ActionType))
        {
            result[group.Key] = group.Count();
        }
        return result;
    }

    public async Task<double> GetSuccessRateAsync(int days = 7, CancellationToken cancellationToken = default)
    {
        var from = DateTime.UtcNow.Date.AddDays(-days);
        var filter = Builders<SimulationLog>.Filter.Gte(x => x.SimulatedDateUtc, from);
        var total = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        if (total == 0) return 0;

        var successFilter = Builders<SimulationLog>.Filter.And(
            Builders<SimulationLog>.Filter.Gte(x => x.SimulatedDateUtc, from),
            Builders<SimulationLog>.Filter.Eq(x => x.Result, SimulationLogResult.Success)
        );

        var success = await _collection.CountDocumentsAsync(successFilter, cancellationToken: cancellationToken);
        return (double)success / total * 100;
    }
}
