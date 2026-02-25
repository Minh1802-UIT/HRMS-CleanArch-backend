using Employee.Application.Features.HumanResource.Dtos;
using MediatR;

namespace Employee.Application.Features.HumanResource.Events
{
    public class EmployeeUpdatedEvent : INotification
    {
        public string EmployeeId { get; }
        public object OldValue { get; }
        public UpdateEmployeeDto NewValue { get; }

        public EmployeeUpdatedEvent(string employeeId, object oldValue, UpdateEmployeeDto newValue)
        {
            EmployeeId = employeeId;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}
