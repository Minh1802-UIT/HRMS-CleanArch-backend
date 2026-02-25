using Employee.API.Common;
using Employee.Application.Features.Common.Queries.GetAuditLogs;
using Employee.Application.Features.Common.Queries.GetAuditLogsCursor;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Employee.API.Endpoints.Common
{
    public static class AuditLogHandlers
    {
        // ------ Offset-based (kept for backward compatibility) ------
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

        // ------ Cursor-based (preferred for large collections) ------
        /// <summary>
        /// Cursor-based pagination endpoint. Avoids Skip(N) on 250 K+ rows.
        /// First page: omit afterCursor.
        /// Subsequent pages: pass the nextCursor value from the previous response.
        /// </summary>
        public static async Task<IResult> GetLogsCursor(
            [FromQuery] string? afterCursor,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? searchTerm = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string? userId = null,
            [FromQuery] string? actionType = null,
            IMediator mediator = default!)
        {
            var query = new GetAuditLogsCursorQuery
            {
                AfterCursor = afterCursor,
                PageSize    = pageSize,
                SearchTerm  = searchTerm,
                StartDate   = startDate,
                EndDate     = endDate,
                UserId      = userId,
                ActionType  = actionType
            };

            var result = await mediator.Send(query);
            return ResultUtils.Success(result, "Audit logs (cursor-paged) retrieved successfully.");
        }
    }
}
