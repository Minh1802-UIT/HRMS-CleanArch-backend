using MediatR;
using Employee.Application.Common.Interfaces; // ICurrentUser
using Employee.Application.Common.Interfaces.Organization.IService; // IAuditLogService
using Employee.Application.Features.HumanResource.Events;

namespace Employee.Application.Features.HumanResource.EventHandlers
{
    public class EmployeeUpdatedEventHandler : INotificationHandler<EmployeeUpdatedEvent>
    {
        private readonly IAuditLogService _auditService;
        private readonly ICurrentUser _currentUser;

        public EmployeeUpdatedEventHandler(IAuditLogService auditService, ICurrentUser currentUser)
        {
            _auditService = auditService;
            _currentUser = currentUser;
        }

        public async Task Handle(EmployeeUpdatedEvent notification, CancellationToken cancellationToken)
        {
            // Ghi Audit Log tách biệt
            await _auditService.LogAsync(
                userId: _currentUser.UserId ?? "System",
                userName: _currentUser.UserName ?? "System",
                action: "UPDATE_EMPLOYEE",
                tableName: "Employees",
                recordId: notification.EmployeeId,
                oldVal: notification.OldValue,
                newVal: new { 
                    Name = notification.NewValue.FullName, 
                    DeptId = notification.NewValue.JobDetails.DepartmentId 
                }
            );
        }
    }
}
