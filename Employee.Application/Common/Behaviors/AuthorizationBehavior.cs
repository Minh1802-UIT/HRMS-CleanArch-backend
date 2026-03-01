using System.Reflection;
using Employee.Application.Common.Interfaces;
using Employee.Application.Common.Security;
using MediatR;

namespace Employee.Application.Common.Behaviors
{
    /// <summary>
    /// MediatR pipeline behavior that enforces authorization at the application layer.
    /// Checks [Authorize] attributes on commands/queries BEFORE the handler runs.
    /// This ensures authorization is enforced even when handlers are invoked
    /// programmatically via ISender (not just through HTTP endpoints).
    /// </summary>
    public class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly ICurrentUser _currentUser;

        public AuthorizationBehavior(ICurrentUser currentUser)
        {
            _currentUser = currentUser;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            var authorizeAttributes = request.GetType()
                .GetCustomAttributes<AuthorizeAttribute>()
                .ToList();

            if (!authorizeAttributes.Any())
                return await next();

            // Must be authenticated
            if (string.IsNullOrEmpty(_currentUser.UserId))
                throw new UnauthorizedAccessException("User is not authenticated.");

            // Check role requirements
            var rolesAttributes = authorizeAttributes
                .Where(a => !string.IsNullOrWhiteSpace(a.Roles))
                .ToList();

            if (rolesAttributes.Any())
            {
                var authorized = false;
                foreach (var attr in rolesAttributes)
                {
                    var roles = attr.Roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    foreach (var role in roles)
                    {
                        if (_currentUser.IsInRole(role))
                        {
                            authorized = true;
                            break;
                        }
                    }
                    if (authorized) break;
                }

                if (!authorized)
                    throw new UnauthorizedAccessException(
                        $"User '{_currentUser.UserName}' does not have the required role(s).");
            }

            return await next();
        }
    }
}
