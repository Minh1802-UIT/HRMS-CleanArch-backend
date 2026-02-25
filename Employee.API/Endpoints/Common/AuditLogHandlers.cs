using Employee.API.Common;
using Employee.Application.Features.Common.Queries.GetAuditLogs;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Employee.API.Endpoints.Common
{
    public static class AuditLogHandlers
    {
        public static async Task<IResult> GetLogs(
            [FromQuery] int? pageNumber,
            [FromQuery] int? pageSize,
            [FromQuery] string? searchTerm,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] string? userId,
            [FromQuery] string? actionType,
            IMediator mediator)
        {
            var query = new GetAuditLogsQuery
            {
                Pagination = new Employee.Application.Common.Models.PaginationParams
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    SearchTerm = searchTerm
                },
                StartDate = startDate,
                EndDate = endDate,
                UserId = userId,
                ActionType = actionType
            };

            var result = await mediator.Send(query);
            return ResultUtils.Success(result, "Audit logs retrieved successfully.");
        }
    }
}
