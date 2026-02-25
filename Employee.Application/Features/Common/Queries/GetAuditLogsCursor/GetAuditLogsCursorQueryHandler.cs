using Employee.Application.Common.Interfaces.Organization.IRepository;
using Employee.Application.Common.Models;
using Employee.Application.Features.Common.Dtos;
using Employee.Application.Features.Common.Mappers;
using MediatR;

namespace Employee.Application.Features.Common.Queries.GetAuditLogsCursor
{
  public class GetAuditLogsCursorQueryHandler
      : IRequestHandler<GetAuditLogsCursorQuery, CursorPagedResult<AuditLogDto>>
  {
    private readonly IAuditLogRepository _repo;

    public GetAuditLogsCursorQueryHandler(IAuditLogRepository repo)
    {
      _repo = repo;
    }

    public async Task<CursorPagedResult<AuditLogDto>> Handle(
        GetAuditLogsCursorQuery request,
        CancellationToken cancellationToken)
    {
      var pageSize = Math.Min(Math.Max(request.PageSize, 1), 100);

      var result = await _repo.GetLogsCursorPagedAsync(
          request.AfterCursor,
          pageSize,
          request.StartDate,
          request.EndDate,
          request.UserId,
          request.ActionType,
          request.SearchTerm,
          cancellationToken);

      return new CursorPagedResult<AuditLogDto>
      {
        Items      = result.Items.Select(l => l.ToDto()).ToList(),
        NextCursor = result.NextCursor,
        PageSize   = result.PageSize
      };
    }
  }
}
