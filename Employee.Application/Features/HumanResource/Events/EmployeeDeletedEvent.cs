using MediatR;

namespace Employee.Application.Features.HumanResource.Events
{
    public class EmployeeDeletedEvent : INotification
    {
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
