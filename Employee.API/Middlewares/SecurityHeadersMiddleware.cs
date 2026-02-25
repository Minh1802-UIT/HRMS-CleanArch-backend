namespace Employee.API.Middlewares
{
  /// <summary>
  /// Adds security headers to every HTTP response:
  /// - Content-Security-Policy (API-appropriate: default-src 'none')
  /// - X-Content-Type-Options (prevent MIME sniffing)
  /// - X-Frame-Options (prevent clickjacking)
  /// - Referrer-Policy (limit referrer info leakage)
  /// - Permissions-Policy (restrict browser feature access)
  /// - X-XSS-Protection (legacy XSS filter for older browsers)
  /// </summary>
  public class SecurityHeadersMiddleware
  {
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
      _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
      var headers = context.Response.Headers;

      // Prevent MIME sniffing — always interpret content as declared Content-Type
      headers["X-Content-Type-Options"] = "nosniff";

      // Prevent clickjacking via iframes
      headers["X-Frame-Options"] = "DENY";

      // Reduce referrer information sent in cross-origin requests
      headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

      // Legacy XSS filter (still useful for older browsers)
      headers["X-XSS-Protection"] = "1; mode=block";

      // Restrict access to browser features
      headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";

      // CSP for REST API: deny all framing/embedding; APIs don't serve documents
      // Scripts/styles are not relevant for a JSON API
      headers["Content-Security-Policy"] =
          "default-src 'none'; frame-ancestors 'none'; form-action 'none'";

      // Inform browsers not to cache sensitive API responses by default
      // (Individual endpoints can override this via [ResponseCache] or Cache-Control headers)
      if (!context.Response.Headers.ContainsKey("Cache-Control"))
      {
        headers["Cache-Control"] = "no-store";
      }

      await _next(context);
    }
  }

  /// <summary>Extension method to register the SecurityHeadersMiddleware cleanly.</summary>
  public static class SecurityHeadersMiddlewareExtensions
  {
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        => app.UseMiddleware<SecurityHeadersMiddleware>();
  }
}
