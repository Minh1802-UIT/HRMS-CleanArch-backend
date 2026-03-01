using System;
using Employee.Domain.Entities.Common;

namespace Employee.Domain.Events
{
    public class EmployeeDeletedEvent : IDomainEvent
    {
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
        public string EmployeeId { get; }
        public string EmployeeCode { get; }
        public string FullName { get; }

        public EmployeeDeletedEvent(string employeeId, string employeeCode, string fullName)
        {
            EmployeeId = employeeId;
            EmployeeCode = employeeCode;
            FullName = fullName;
        }
    }
}
