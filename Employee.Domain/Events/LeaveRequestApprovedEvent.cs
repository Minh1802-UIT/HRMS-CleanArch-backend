using System;
using Employee.Domain.Entities.Common;

namespace Employee.Domain.Events
{
    public record LeaveRequestApprovedEvent(
        string LeaveRequestId,
        string EmployeeId,
        string ApprovedBy,
        string? ManagerComment,
        double WorkingDaysDeducted
    ) : IDomainEvent
    {
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }
}
