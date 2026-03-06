using Employee.Application.Common.Exceptions;
using Employee.Application.Common.Interfaces;
using Employee.Application.Common.Interfaces.Organization.IService;
using MediatR;

namespace Employee.Application.Features.Auth.Commands.AssignRole
{
    public class AssignRoleCommandHandler : IRequestHandler<AssignRoleCommand>
    {
    private readonly IIdentityService _identityService;
        private readonly IAuditLogService _auditService;
        private readonly ICurrentUser _currentUser;

        public AssignRoleCommandHandler(
            IIdentityService identityService,
            IAuditLogService auditService,
            ICurrentUser currentUser)
        {
      _identityService = identityService;
            _auditService = auditService;
            _currentUser = currentUser;
        }

        public async Task Handle(AssignRoleCommand request, CancellationToken cancellationToken)
        {
      // 1. Get User by Username
      var user = await _identityService.GetUserByUsernameAsync(request.Username);
            if (user == null) throw new NotFoundException($"User '{request.Username}' not found.");

            // 2. Assign role
            var result = await _identityService.AssignRoleAsync(user.Id, request.RoleName);

      if (!result.Succeeded)
      {
                throw new ValidationException($"Failed to assign role to user '{request.Username}': {string.Join(", ", result.Errors)}");
            }

      // 3. Audit Log
      var adminId = _currentUser.UserId ?? "System";
            var adminName = _currentUser.UserName ?? "System_Admin";

            await _auditService.LogAsync(
                userId: adminId,
                userName: adminName,
                action: "ASSIGN_ROLE",
                tableName: "Users",
                recordId: user.Id,
                oldVal: new { PreviousRoles = user.Roles },
                newVal: new { AddedRole = request.RoleName }
            );
        }
    }
}
