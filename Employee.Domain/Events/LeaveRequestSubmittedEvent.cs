using System;
using Employee.Domain.Entities.Common;

namespace Employee.Domain.Events
{
    public record LeaveRequestSubmittedEvent(
        string LeaveRequestId,
        string EmployeeId,
        string LeaveType,
        DateTime FromDate,
        DateTime ToDate,
        string Reason
    ) : IDomainEvent
    {
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }
}
