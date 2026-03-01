using Employee.Application.Common.Dtos;
using Employee.Domain.Common.Models;

namespace Employee.Application.Common.Interfaces;

/// <summary>
/// Application-layer extension of IEmployeeRepository for projection queries that
/// return Application-layer DTOs (not Domain entities). Keeping these methods here
/// prevents the Domain layer from referencing Application-layer types.
/// Implemented by Employee.Infrastructure.Repositories.HumanResource.EmployeeRepository.
/// </summary>
public interface IEmployeeQueryRepository
{
    /// <summary>
    /// Projection-only paged query for the Employee list page.
    /// Fetches only the fields rendered on the list (~500 bytes vs ~5 KB per document).
    /// </summary>
    Task<PagedResult<EmployeeListSummary>> GetPagedListAsync(
        PaginationParams pagination,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Search-as-you-type lookup for dropdowns.
    /// </summary>
    Task<List<LookupDto>> GetLookupAsync(
        string? keyword = null,
        int limit = 20,
        CancellationToken cancellationToken = default);
}
