using Employee.Application.Common.Models;
using MediatR;
using Employee.Application.Common.Interfaces; // ICurrentUser
using Employee.Application.Common.Interfaces.Organization.IService; // IAuditLogService
using Employee.Domain.Events;

namespace Employee.Application.Features.HumanResource.EventHandlers
{
    public class EmployeeUpdatedEventHandler : INotificationHandler<DomainEventNotification<EmployeeUpdatedEvent>>
    {
        private readonly IAuditLogService _auditService;
        private readonly ICurrentUser _currentUser;
        private readonly IIdentityService _identityService;

        public EmployeeUpdatedEventHandler(IAuditLogService auditService, ICurrentUser currentUser, IIdentityService identityService)
        {
            _auditService = auditService;
            _currentUser = currentUser;
            _identityService = identityService;
        }

        public async Task Handle(DomainEventNotification<EmployeeUpdatedEvent> notificationWrapper, CancellationToken cancellationToken)
        {
            // Ghi Audit Log tách biệt
            await _auditService.LogAsync(
                userId: _currentUser.UserId ?? "System",
                userName: _currentUser.UserName ?? "System",
                action: "UPDATE_EMPLOYEE",
                tableName: "Employees",
                recordId: notificationWrapper.DomainEvent.EmployeeId,
                oldVal: notificationWrapper.DomainEvent.OldValuesJson,
                newVal: notificationWrapper.DomainEvent.NewValuesJson
            );

            // Sync Identity User
            try
            {
                var newValues = System.Text.Json.JsonDocument.Parse(notificationWrapper.DomainEvent.NewValuesJson);
                var fullName = newValues.RootElement.TryGetProperty("FullName", out var fn) ? fn.GetString() : null;
                var email = newValues.RootElement.TryGetProperty("Email", out var em) ? em.GetString() : null;

                if (!string.IsNullOrEmpty(fullName) && !string.IsNullOrEmpty(email))
                {
                    await _identityService.SyncUserFromEmployeeAsync(notificationWrapper.DomainEvent.EmployeeId, fullName, email);
                }
            }
            catch (Exception ex)
            {
                // Silently log or ignore json parse errors to not break the event handler
                Console.WriteLine($"[EmployeeUpdatedEventHandler] Failed to sync identity user: {ex.Message}");
            }
        }
    }
}

