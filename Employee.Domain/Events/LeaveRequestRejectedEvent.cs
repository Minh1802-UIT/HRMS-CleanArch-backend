using System;
using Employee.Domain.Entities.Common;

namespace Employee.Domain.Events
{
    public record LeaveRequestRejectedEvent(
        string LeaveRequestId,
        string EmployeeId,
        string RejectedBy,
        string? ManagerComment
    ) : IDomainEvent
    {
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }
}
