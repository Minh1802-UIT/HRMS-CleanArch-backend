using Employee.Domain.Common.Models;
using Employee.Application.Features.Common.Dtos;
using MediatR;

namespace Employee.Application.Features.Common.Queries.GetAuditLogsCursor
{
  public class GetAuditLogsCursorQuery : IRequest<CursorPagedResult<AuditLogDto>>
  {
    /// <summary>
    /// Opaque cursor from the previous page's <c>NextCursor</c>.
    /// Pass null (or omit) to load the first page.
    /// </summary>
    public string? AfterCursor { get; set; }

    /// <summary>Items per page. Clamped to [1, 100] to prevent DoS via oversized requests.</summary>
    private int _pageSize = 20;
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = Math.Min(Math.Max(value, 1), 100);
    }

    public DateTime? StartDate  { get; set; }
    public DateTime? EndDate    { get; set; }
    public string?   UserId     { get; set; }
    public string?   ActionType { get; set; }
    public string?   SearchTerm { get; set; }
  }
}
