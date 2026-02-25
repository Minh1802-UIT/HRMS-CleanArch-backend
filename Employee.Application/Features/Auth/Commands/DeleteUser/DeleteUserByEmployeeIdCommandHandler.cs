using Employee.Application.Common.Exceptions;
using Employee.Application.Common.Interfaces;
using Employee.Application.Common.Interfaces.Organization.IService;
using MediatR;

namespace Employee.Application.Features.Auth.Commands.DeleteUser
{
    public class DeleteUserByEmployeeIdCommandHandler : IRequestHandler<DeleteUserByEmployeeIdCommand>
    {
    private readonly IIdentityService _identityService;
        private readonly IAuditLogService _auditService;
        private readonly ICurrentUser _currentUser;

        public DeleteUserByEmployeeIdCommandHandler(
            IIdentityService identityService,
            IAuditLogService auditService,
            ICurrentUser currentUser)
        {
      _identityService = identityService;
            _auditService = auditService;
            _currentUser = currentUser;
        }

        public async Task Handle(DeleteUserByEmployeeIdCommand request, CancellationToken cancellationToken)
    {
            var adminId = _currentUser.UserId ?? "System";
            var adminName = _currentUser.UserName ?? "System_Admin";

      var result = await _identityService.DeleteByEmployeeIdAsync(request.EmployeeId);

      if (!result.Succeeded)
      {
        throw new ValidationException($"Không thể vô hiệu hóa User cho EmployeeId '{request.EmployeeId}': {string.Join(", ", result.Errors)}");
      }

      // Log deactivation
      await _auditService.LogAsync(
          userId: adminId,
          userName: adminName,
          action: "DEACTIVATE_USER_ON_DELETE",
          tableName: "Users",
                recordId: request.EmployeeId, // Use EmpId as record identifier here
                oldVal: new { EmpId = request.EmployeeId, Status = "Active" },
                newVal: new { Status = "Deactivated (Locked Out)" }
            );
        }
    }
}
