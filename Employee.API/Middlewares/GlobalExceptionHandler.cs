using Employee.Application.Common.Wrappers;
using Employee.API.Common;
using Employee.Domain.Constants;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Employee.API.Middlewares
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;
        private readonly IHostEnvironment _env;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment env)
        {
            _logger = logger;
            _env = env;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "Unhandled exception occurred: {Message}", exception.Message);

            // 1. Determine Error Code and Status
            var (statusCode, errorCode, title, errors, isKnown) = exception switch
            {
                Employee.Application.Common.Exceptions.NotFoundException =>
                    (StatusCodes.Status404NotFound, ErrorCodes.NotFound("DATA"), "Resource not found", null, true),

                Employee.Application.Common.Exceptions.ValidationException valEx =>
                    (StatusCodes.Status400BadRequest, ErrorCodes.InvalidData, "Validation failed", valEx.Errors, true),

                Employee.Application.Common.Exceptions.ConflictException =>
                    (StatusCodes.Status409Conflict, ErrorCodes.Conflict("DATA"), "Data conflict", null, true),

                Employee.Application.Common.Exceptions.ConcurrencyException =>
                    (StatusCodes.Status409Conflict, "CONCURRENCY_ERROR", "Data has been modified by another user", null, true),

                // Handle BusinessRuleViolationException → 422
                Employee.Application.Common.Exceptions.BusinessRuleViolationException =>
                    (StatusCodes.Status422UnprocessableEntity, "BUSINESS_RULE_VIOLATION", "Business rule violated", null, true),

                Employee.Application.Common.Exceptions.ForbiddenException =>
                    (StatusCodes.Status403Forbidden, "FORBIDDEN", "Access denied", null, true),

                UnauthorizedAccessException =>
                    (StatusCodes.Status401Unauthorized, ErrorCodes.Unauthorized, "Unauthorized access", null, true),

                // Domain state-transition violations (e.g. mark paid on Draft payroll)
                InvalidOperationException =>
                    (StatusCodes.Status422UnprocessableEntity, "INVALID_OPERATION", "Invalid operation", null, true),

                _ => (StatusCodes.Status500InternalServerError, ErrorCodes.InternalError, "Internal system error", null, false)
            };

            // 2. SECURITY: Never expose raw exception details for unhandled errors in production.
            //    Known domain exceptions carry user-safe messages, so always show those.
            //    Unknown (500) exceptions only expose detail in Development to aid debugging.
            var userMessage = (isKnown || _env.IsDevelopment())
                ? exception.Message
                : "An unexpected error occurred. Please contact support if the problem persists.";

            // 3. Create Unified ApiResponse
            var response = ApiResponse<object?>.FailResult(errorCode, userMessage, errors);

            // 4. Set Response Header & Body
            httpContext.Response.StatusCode = statusCode;
            httpContext.Response.ContentType = "application/json";
            await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

            return true;
        }
    }
}
