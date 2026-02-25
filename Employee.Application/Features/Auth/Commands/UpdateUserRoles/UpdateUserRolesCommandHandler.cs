using Employee.Application.Common.Exceptions;
using Employee.Application.Common.Interfaces;
using Employee.Application.Common.Interfaces.Organization.IService;
using MediatR;

namespace Employee.Application.Features.Auth.Commands.UpdateUserRoles
{
    public class UpdateUserRolesCommandHandler : IRequestHandler<UpdateUserRolesCommand>
    {
    private readonly IIdentityService _identityService;
        private readonly IAuditLogService _auditService;
        private readonly ICurrentUser _currentUser;

        public UpdateUserRolesCommandHandler(
            IIdentityService identityService,
            IAuditLogService auditService,
            ICurrentUser currentUser)
        {
      _identityService = identityService;
            _auditService = auditService;
            _currentUser = currentUser;
        }

        public async Task Handle(UpdateUserRolesCommand request, CancellationToken cancellationToken)
        {
      // Update roles via IdentityService
      var result = await _identityService.UpdateUserRolesAsync(request.UserId, request.RoleNames);

      if (!result.Succeeded)
            {
        throw new ValidationException(string.Join(", ", result.Errors));
      }

      // Audit Log
      var adminId = _currentUser.UserId ?? "System";
      var adminName = _currentUser.UserName ?? "System_Admin";

      await _auditService.LogAsync(
          userId: adminId,
          userName: adminName,
          action: "SYNC_ROLES",
          tableName: "Users",
          recordId: request.UserId,
                oldVal: new { Note = "Roles updated" },
                newVal: new { Roles = request.RoleNames }
            );
        }
    }
}
