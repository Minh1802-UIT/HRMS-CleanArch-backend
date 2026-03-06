using Employee.Application.Common.Wrappers;

namespace Employee.API.Common
{
  public static class ResultUtils
  {
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

      // If the caller provides an explicit status code use it;
      // otherwise infer from the errorCode naming convention.
      int finalStatusCode = statusCode ?? GetStatusCodeByConvention(errorCode);

      return Results.Json(response, statusCode: finalStatusCode);
    }

    // Infers an HTTP status code from the errorCode naming convention.
    // Only the suffix matters — works for any domain (Employee, Payroll, Leave, etc.).
    public static int GetStatusCodeByConvention(string errorCode)
    {
      if (string.IsNullOrEmpty(errorCode)) return 400;

      var code = errorCode.ToUpper();

      // 404: codes ending with _NOT_FOUND
      if (code.EndsWith("_NOT_FOUND")) return 404;

      // 409: codes ending with _EXIST, _CONFLICT, or _DUPLICATE
      if (code.EndsWith("_EXIST") || code.EndsWith("_CONFLICT") || code.EndsWith("_DUPLICATE")) return 409;

      // 401: authentication required
      if (code.EndsWith("_UNAUTHORIZED") || code.EndsWith("_REQUIRED")) return 401;

      // 403: access denied
      if (code.EndsWith("_FORBIDDEN") || code.EndsWith("_DENIED")) return 403;

      // 500: server-side faults
      if (code.EndsWith("_INTERNAL_ERROR") || code.EndsWith("_SERVER_ERROR")) return 500;

      // 400: input / validation errors
      if (code.EndsWith("_INVALID") ||
          code.EndsWith("_MISSING") ||
          code.EndsWith("_FAILED") ||
          code.EndsWith("_BAD_REQUEST"))
      {
        return 400;
      }

      // Default: 400 for any unrecognised code
      return 400;
    }

    // 4. Created (201) — pass a non-null 'location' to emit a Location response header.
    public static IResult Created<T>(T data, string message = "Created successfully", string? location = null)
    {
      var body = ApiResponse<T>.SuccessResult(data, message);
      return location != null
        ? Results.Created(location, body)
        : Results.Json(body, statusCode: 201);
    }

    /// <summary>201 Created with no data body. Use Created&lt;object?&gt;(null, message) for message-only responses.</summary>
    public static IResult CreatedNoData(string message = "Created successfully")
    {
      return Created<object?>(null, message);
    }
  }
}