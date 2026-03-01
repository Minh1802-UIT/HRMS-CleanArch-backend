using System;
using Employee.Domain.Entities.Common;

namespace Employee.Domain.Events
{
    public class ContractCreatedEvent : IDomainEvent
    {
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
        public string EmployeeId { get; }
        public string ContractId { get; }

        public ContractCreatedEvent(string employeeId, string contractId)
        {
            EmployeeId = employeeId;
            ContractId = contractId;
        }
    }
}
