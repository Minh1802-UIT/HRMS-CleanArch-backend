using MediatR;
using Employee.Application.Features.HumanResource.Dtos;

namespace Employee.Application.Features.HumanResource.Events
{
    public class ContractCreatedEvent : INotification
    {
        public string EmployeeId { get; }
        public string ContractId { get; }

        public ContractCreatedEvent(string employeeId, string contractId)
        {
            EmployeeId = employeeId;
            ContractId = contractId;
        }
    }
}
