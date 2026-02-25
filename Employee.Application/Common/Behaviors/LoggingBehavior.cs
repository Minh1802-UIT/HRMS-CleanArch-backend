using System.Diagnostics;
using Employee.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Employee.Application.Common.Behaviors
{
  /// <summary>
  /// MediatR pipeline behavior that instruments every command/query with:
  ///   • Structured logging (start, success, slow-query warning, error)
  ///   • Wall-clock execution time via <see cref="Stopwatch"/>
  ///   • Correlation ID (distributed tracing) pushed onto the log scope
  ///   • Authenticated user ID pushed onto the log scope
  ///
  /// Registered BEFORE <see cref="ValidationBehavior{TRequest,TResponse}"/> so
  /// it wraps the entire pipeline — timing includes validation overhead.
  ///
  /// Slow-query threshold: 500 ms → logged at Warning.
  /// </summary>
  public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
      where TRequest : notnull
  {
    private const int SlowThresholdMs = 500;

    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    private readonly ICurrentUser _currentUser;
    private readonly ICorrelationIdProvider _correlationIdProvider;

    public LoggingBehavior(
        ILogger<LoggingBehavior<TRequest, TResponse>> logger,
        ICurrentUser currentUser,
        ICorrelationIdProvider correlationIdProvider)
    {
      _logger = logger;
      _currentUser = currentUser;
      _correlationIdProvider = correlationIdProvider;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
      var requestName = typeof(TRequest).Name;
      var correlationId = _correlationIdProvider.GetCorrelationId();
      var userId = _currentUser.UserId;

      // Push structured properties onto the logger scope so every log line
      // emitted from within the handler automatically carries them.
      // Serilog (via ILogger bridge) and Application Insights both honour
      // ILogger.BeginScope key-value pairs.
      using (_logger.BeginScope(new Dictionary<string, object>
      {
        ["CorrelationId"] = correlationId,
        ["UserId"] = userId ?? "anonymous",
        ["RequestName"] = requestName
      }))
      {
        _logger.LogInformation(
            "→ Handling {RequestName} | CorrelationId: {CorrelationId} | User: {UserId}",
            requestName, correlationId, userId ?? "anonymous");

        var sw = Stopwatch.StartNew();

        try
        {
          var response = await next();
          sw.Stop();

          if (sw.ElapsedMilliseconds >= SlowThresholdMs)
          {
            _logger.LogWarning(
                "⚠ SLOW {RequestName} | {ElapsedMs} ms | CorrelationId: {CorrelationId} | User: {UserId}",
                requestName, sw.ElapsedMilliseconds, correlationId, userId ?? "anonymous");
          }
          else
          {
            _logger.LogInformation(
                "✓ Completed {RequestName} | {ElapsedMs} ms | CorrelationId: {CorrelationId} | User: {UserId}",
                requestName, sw.ElapsedMilliseconds, correlationId, userId ?? "anonymous");
          }

          return response;
        }
        catch (Exception ex)
        {
          sw.Stop();
          _logger.LogError(
              ex,
              "✗ Failed {RequestName} | {ElapsedMs} ms | CorrelationId: {CorrelationId} | User: {UserId}",
              requestName, sw.ElapsedMilliseconds, correlationId, userId ?? "anonymous");

          throw; // Re-throw — do not swallow
        }
      }
    }
  }
}
