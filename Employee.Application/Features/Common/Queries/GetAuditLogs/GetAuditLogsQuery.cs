using Employee.Domain.Common.Models;
using Employee.Application.Features.Common.Dtos;
using MediatR;

namespace Employee.Application.Features.Common.Queries.GetAuditLogs
{
    public class GetAuditLogsQuery : IRequest<PagedResult<AuditLogDto>>
    {
        public PaginationParams Pagination { get; set; } = new();
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? UserId { get; set; }
        public string? ActionType { get; set; }
    }
}
