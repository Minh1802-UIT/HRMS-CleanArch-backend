using Employee.Application.Common.Interfaces;
using System.Diagnostics;

namespace Employee.API.Services
{
  /// <summary>
  /// Reads the Correlation ID from the incoming request headers, or generates
  /// a new one when absent.  The resolved value is cached in
  /// <see cref="HttpContext.Items"/> so it stays stable for the whole request.
  ///
  /// Header precedence:
  ///   1. X-Correlation-ID   (de-facto distributed tracing standard)
  ///   2. X-Request-ID       (legacy / ALB style)
  ///   3. Activity.TraceId   (OpenTelemetry W3C trace context)
  ///   4. New GUID (16-char hex) — generated here and attached to the response
  /// </summary>
  public class CorrelationIdProvider : ICorrelationIdProvider
  {
    private const string CacheKey        = "CorrelationId";
    private const string RequestHeader   = "X-Correlation-ID";
    private const string FallbackHeader  = "X-Request-ID";
    private const string ResponseHeader  = "X-Correlation-ID";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationIdProvider(IHttpContextAccessor httpContextAccessor)
    {
      _httpContextAccessor = httpContextAccessor;
    }

    public string GetCorrelationId()
    {
      var context = _httpContextAccessor.HttpContext;

      // Non-HTTP path (background jobs, tests without HTTP context)
      if (context == null)
      {
        return Activity.Current?.TraceId.ToString()
            ?? Guid.NewGuid().ToString("N")[..16];
      }

      // Already resolved for this request — return cached value
      if (context.Items.TryGetValue(CacheKey, out var cached) && cached is string existing)
        return existing;

      // Try to read from incoming request headers
      var correlationId =
          context.Request.Headers[RequestHeader].FirstOrDefault()?.Trim()
          ?? context.Request.Headers[FallbackHeader].FirstOrDefault()?.Trim();

      // Fall back to OpenTelemetry trace ID, then a fresh GUID
      if (string.IsNullOrEmpty(correlationId))
      {
        correlationId = Activity.Current?.TraceId.ToString()
            ?? Guid.NewGuid().ToString("N")[..16];
      }

      // Cache for the remainder of this request
      context.Items[CacheKey] = correlationId;

      // Echo back in response so clients can correlate logs
      if (!context.Response.HasStarted)
        context.Response.Headers[ResponseHeader] = correlationId;

      return correlationId;
    }
  }
}
