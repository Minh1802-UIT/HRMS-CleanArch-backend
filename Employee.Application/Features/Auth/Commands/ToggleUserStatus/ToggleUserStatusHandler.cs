using Employee.Application.Common.Exceptions;
using Employee.Application.Common.Interfaces;
using Employee.Application.Common.Interfaces.Organization.IService;
using MediatR;

namespace Employee.Application.Features.Auth.Commands.ToggleUserStatus
{
    public class ToggleUserStatusHandler : IRequestHandler<ToggleUserStatusCommand>
    {
        private readonly IIdentityService _identityService;
        private readonly IAuditLogService _auditService;
        private readonly ICurrentUser _currentUser;

        public ToggleUserStatusHandler(
            IIdentityService identityService,
            IAuditLogService auditService,
            ICurrentUser currentUser)
        {
            _identityService = identityService;
            _auditService = auditService;
            _currentUser = currentUser;
        }

        public async Task Handle(ToggleUserStatusCommand request, CancellationToken cancellationToken)
        {
            var result = await _identityService.ToggleUserStatusAsync(request.UserId, request.IsActive);

            if (!result.Succeeded)
            {
                throw new ValidationException(string.Join(", ", result.Errors));
            }

            var adminId = _currentUser.UserId ?? "System";
            var adminName = _currentUser.UserName ?? "System_Admin";

            await _auditService.LogAsync(
                userId: adminId,
                userName: adminName,
                action: request.IsActive ? "ACTIVATE_USER" : "DEACTIVATE_USER",
                tableName: "Users",
                recordId: request.UserId,
                oldVal: new { IsActive = !request.IsActive },
                newVal: new { IsActive = request.IsActive }
            );
        }
    }
}
