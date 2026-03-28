using Employee.Domain.Common.Models;
using Employee.Domain.Entities.Simulation;
using Employee.Domain.Interfaces.Repositories;

namespace Employee.Domain.Interfaces.Repositories;

public interface ISimulationBotRepository : IBaseRepository<SimulationBot>
{
    Task<List<SimulationBot>> GetActiveBotsAsync(CancellationToken cancellationToken = default);
    Task<SimulationBot?> GetByEmployeeIdAsync(string employeeId, CancellationToken cancellationToken = default);
    Task<List<SimulationBot>> GetByIdsAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default);
    Task<long> CountActiveAsync(CancellationToken cancellationToken = default);
    Task<long> CountByStatusAsync(SimulationBotStatus status, CancellationToken cancellationToken = default);
}

public interface ISimulationLogRepository : IBaseRepository<SimulationLog>
{
    Task<List<SimulationLog>> GetByBotIdAsync(string botId, int limit = 100, CancellationToken cancellationToken = default);
    Task<List<SimulationLog>> GetByDateAsync(DateTime dateUtc, CancellationToken cancellationToken = default);
    Task<List<SimulationLog>> GetByDateRangeAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
    Task<Dictionary<SimulationActionType, int>> GetActionStatsAsync(int days = 7, CancellationToken cancellationToken = default);
    Task<double> GetSuccessRateAsync(int days = 7, CancellationToken cancellationToken = default);
}
