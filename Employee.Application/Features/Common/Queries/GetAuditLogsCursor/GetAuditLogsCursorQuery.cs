using Employee.Application.Common.Models;
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

    /// <summary>Items per page. Capped at 100.</summary>
    public int PageSize { get; set; } = 20;

    public DateTime? StartDate  { get; set; }
    public DateTime? EndDate    { get; set; }
    public string?   UserId     { get; set; }
    public string?   ActionType { get; set; }
    public string?   SearchTerm { get; set; }
  }
}
