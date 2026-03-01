using Employee.Domain.Interfaces.Repositories;
using Employee.Domain.Common.Models;
using Employee.Application.Features.Common.Dtos;
using Employee.Application.Features.Common.Mappers;
using MediatR;

namespace Employee.Application.Features.Common.Queries.GetAuditLogs
{
    public class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, PagedResult<AuditLogDto>>
    {
        private readonly IAuditLogRepository _auditLogRepository;

        public GetAuditLogsQueryHandler(IAuditLogRepository auditLogRepository)
        {
            _auditLogRepository = auditLogRepository;
        }

        public async Task<PagedResult<AuditLogDto>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
        {
            var (logs, totalCount) = await _auditLogRepository.GetLogsPagedAsync(
                request.Pagination,
                request.StartDate,
                request.EndDate,
                request.UserId,
                request.ActionType,
                cancellationToken
            );

            var dtos = logs.Select(l => l.ToDto()).ToList();

            return new PagedResult<AuditLogDto>
            {
                Items = dtos,
                TotalCount = (int)totalCount,
                PageNumber = request.Pagination.PageNumber ?? 1,
                PageSize = request.Pagination.PageSize ?? 20
            };
        }
    }
}
