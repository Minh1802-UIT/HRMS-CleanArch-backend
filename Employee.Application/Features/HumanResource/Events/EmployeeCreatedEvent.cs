using MediatR;
using Employee.Application.Features.HumanResource.Dtos; // For EmployeeDto

namespace Employee.Application.Features.HumanResource.Events
{
    public class EmployeeCreatedEvent : INotification
    {
        public EmployeeDto Employee { get; }

        public EmployeeCreatedEvent(EmployeeDto employee)
        {
            Employee = employee;
        }
    }
}
