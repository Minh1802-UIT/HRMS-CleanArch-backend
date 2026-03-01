using System;
using Employee.Domain.Entities.Common;

namespace Employee.Domain.Events
{
    public class EmployeeCreatedEvent : IDomainEvent
    {
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
        public string EmployeeId { get; }
        public string FullName { get; }
        public string Email { get; }
        public string Phone { get; }

        public EmployeeCreatedEvent(string employeeId, string fullName, string email, string phone)
        {
            EmployeeId = employeeId;
            FullName = fullName;
            Email = email;
            Phone = phone;
        }
    }
}
