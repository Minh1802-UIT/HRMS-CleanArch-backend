namespace Employee.Application.Common.Security
{
    /// <summary>
    /// Marker attribute for MediatR requests that require authorization.
    /// Applied at the command/query level so authorization is enforced
    /// even when handlers are invoked programmatically (e.g., from background services).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public sealed class AuthorizeAttribute : Attribute
    {
        /// <summary>
        /// Comma-separated list of roles that are allowed to execute this request.
        /// If empty, any authenticated user is allowed.
        /// </summary>
        public string Roles { get; set; } = string.Empty;
    }
}
