using Employee.Domain.Interfaces.Repositories;
using Employee.Domain.Entities.Simulation;
using Employee.Infrastructure.Persistence;
using Employee.Infrastructure.Repositories.Common;
using MongoDB.Driver;

namespace Employee.Infrastructure.Repositories.Simulation;

public class SimulationBotRepository : BaseRepository<SimulationBot>, ISimulationBotRepository
{
    public SimulationBotRepository(IMongoContext context) : base(context, "simulation_bots")
    {
    }

    public async Task<List<SimulationBot>> GetActiveBotsAsync(CancellationToken cancellationToken = default)
    {
        var filter = Builders<SimulationBot>.Filter.And(
            Builders<SimulationBot>.Filter.Eq(x => x.Status, SimulationBotStatus.Active),
            Builders<SimulationBot>.Filter.Eq(x => x.IsDeleted, false)
        );
        return await _collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<SimulationBot?> GetByEmployeeIdAsync(string employeeId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<SimulationBot>.Filter.And(
            Builders<SimulationBot>.Filter.Eq(x => x.EmployeeId, employeeId),
            Builders<SimulationBot>.Filter.Eq(x => x.IsDeleted, false)
        );
        return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<SimulationBot>> GetByIdsAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        if (idList.Count == 0) return new List<SimulationBot>();

        var filter = Builders<SimulationBot>.Filter.And(
            Builders<SimulationBot>.Filter.In(x => x.Id, idList),
            Builders<SimulationBot>.Filter.Eq(x => x.IsDeleted, false)
        );
        return await _collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<long> CountActiveAsync(CancellationToken cancellationToken = default)
    {
        var filter = Builders<SimulationBot>.Filter.And(
            Builders<SimulationBot>.Filter.Eq(x => x.Status, SimulationBotStatus.Active),
            Builders<SimulationBot>.Filter.Eq(x => x.IsDeleted, false)
        );
        return await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
    }

    public async Task<long> CountByStatusAsync(SimulationBotStatus status, CancellationToken cancellationToken = default)
    {
        var filter = Builders<SimulationBot>.Filter.And(
            Builders<SimulationBot>.Filter.Eq(x => x.Status, status),
            Builders<SimulationBot>.Filter.Eq(x => x.IsDeleted, false)
        );
        return await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
    }
}
