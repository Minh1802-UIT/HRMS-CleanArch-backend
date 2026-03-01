using Employee.Application.Common.Dtos;

namespace Employee.Application.Common.Interfaces;

/// <summary>
/// Application-layer extension of IContractRepository for projection queries that
/// return Application-layer DTOs (not Domain entities).
/// Implemented by Employee.Infrastructure.Repositories.HumanResource.ContractRepository.
/// </summary>
public interface IContractQueryRepository
{
    /// <summary>
    /// Returns salary components for all active contracts, used during payroll calculation.
    /// </summary>
    Task<List<ContractSalaryProjection>> GetActiveSalaryInfoAsync(CancellationToken cancellationToken = default);
}
