namespace Employee.Application.Common.Interfaces
{
  /// <summary>
  /// Provides Correlation ID for the current operation.
  /// Reads X-Correlation-ID / X-Request-ID request headers, falls back
  /// to Activity.TraceId (OpenTelemetry) or a generated GUID.
  /// Implemented in Employee.API to avoid an ASP.NET Core reference in the
  /// Application layer.
  /// </summary>
  public interface ICorrelationIdProvider
  {
    /// <summary>Returns the correlation ID for the current request/operation.</summary>
    string GetCorrelationId();
  }
}
