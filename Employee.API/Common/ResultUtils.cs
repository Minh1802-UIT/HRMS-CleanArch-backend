using Employee.Application.Common.Wrappers;
using Microsoft.Extensions.Logging;

namespace Employee.API.Common
{
  public static class ResultUtils
  {
    private static ILogger? _logger;

    public static void SetLogger(ILogger logger) => _logger ??= logger;

    // 1. Success có Data
    public static IResult Success<T>(T data, string message = "Success")
    {
      return Results.Ok(ApiResponse<T>.SuccessResult(data, message));
    }

    // 2. Success with no data payload (e.g. after a delete)
    public static IResult Success(string message = "Success")
    {
      return Results.Ok(ApiResponse<object?>.SuccessResult(null, message));
    }

    // 3. Fail (Quan trọng)
    public static IResult Fail(string errorCode, string devMessage, int? statusCode = null, List<string>? errors = null)
    {
      var response = ApiResponse<object?>.FailResult(errorCode, devMessage, errors);
      int finalStatusCode = statusCode ?? GetStatusCodeByConvention(errorCode);
      return Results.Json(response, statusCode: finalStatusCode);
    }

    // Infers an HTTP status code from the errorCode naming convention.
    // Uses ErrorCodes.* constants for a complete list of known codes.
    public static int GetStatusCodeByConvention(string errorCode)
    {
      if (string.IsNullOrEmpty(errorCode)) return 400;

      var code = errorCode.ToUpper();

      if (code.EndsWith("_NOT_FOUND")) return 404;
      if (code.EndsWith("_EXIST") || code.EndsWith("_CONFLICT") || code.EndsWith("_DUPLICATE")) return 409;
      if (code.EndsWith("_UNAUTHORIZED") || code.EndsWith("_REQUIRED")) return 401;
      if (code.EndsWith("_FORBIDDEN") || code.EndsWith("_DENIED")) return 403;
      if (code.EndsWith("_INTERNAL_ERROR") || code.EndsWith("_SERVER_ERROR")) return 500;
      if (code.Contains("_UNLINKED")) return 403;
      if (code.EndsWith("_INVALID") || code.EndsWith("_MISSING") ||
          code.EndsWith("_FAILED") || code.EndsWith("_BAD_REQUEST"))
      {
        return 400;
      }

      // Unknown error code — log warning and default to 400
      _logger?.LogWarning(
        "Unknown error code '{ErrorCode}' — defaulting to 400. " +
        "Consider adding it to the ErrorCodes static class in ResultUtils.cs.",
        errorCode);
      return 400;
    }

    // 4. Created (201)
    public static IResult Created<T>(T data, string message = "Created successfully", string? location = null)
    {
      var body = ApiResponse<T>.SuccessResult(data, message);
      return location != null
        ? Results.Created(location, body)
        : Results.Json(body, statusCode: 201);
    }

    public static IResult CreatedNoData(string message = "Created successfully")
    {
      return Created<object?>(null, message);
    }
  }
}