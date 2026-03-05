using MiniValidation;
using Employee.Application.Common.Wrappers;

namespace Employee.API.Common;

/// <summary>
/// Endpoint filter that validates the request body DTO of type <typeparamref name="T"/>
/// using MiniValidator. Returns a 400 ApiResponse with field-level errors on failure.
/// </summary>
public class ValidationFilter<T> : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        // Locate the DTO of type T in the handler's parameter list
        var arg = context.Arguments.FirstOrDefault(x => x?.GetType() == typeof(T));

        if (arg is null) return Results.BadRequest("Invalid arguments");

        // Validate with MiniValidator
        if (!MiniValidator.TryValidate(arg, out var validationErrors))
        {
            // Return 400 using the standard ApiResponse envelope for consistency
            var errorMessages = validationErrors
                .SelectMany(kvp => kvp.Value.Select(msg => $"{kvp.Key}: {msg}"))
                .ToList();
            var response = ApiResponse<object?>.FailResult("VALIDATION_ERROR", "One or more validation errors occurred.", errorMessages);
            return Results.Json(response, statusCode: 400);
        }

        // Validation passed; proceed to handler
        return await next(context);
    }
}