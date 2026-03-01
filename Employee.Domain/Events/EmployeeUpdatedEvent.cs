using System;
using Employee.Domain.Entities.Common;

namespace Employee.Domain.Events
{
    public class EmployeeUpdatedEvent : IDomainEvent
    {
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
        public string EmployeeId { get; }
        public string OldValuesJson { get; }
        public string NewValuesJson { get; }

        public EmployeeUpdatedEvent(string employeeId, string oldValuesJson, string newValuesJson)
        {
            EmployeeId = employeeId;
            OldValuesJson = oldValuesJson;
            NewValuesJson = newValuesJson;
        }
    }
}
