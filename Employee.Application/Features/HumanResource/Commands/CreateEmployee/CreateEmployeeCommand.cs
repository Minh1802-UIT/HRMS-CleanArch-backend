using Employee.Application.Features.HumanResource.Dtos;
using MediatR;

namespace Employee.Application.Features.HumanResource.Commands.CreateEmployee
{
    // Command trả về EmployeeDto (giống như Service cũ)
    public class CreateEmployeeCommand : IRequest<EmployeeDto>
    {
        public string EmployeeCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }

        public PersonalInfoDto PersonalInfo { get; set; } = new();
        public JobDetailsDto JobDetails { get; set; } = new();
        public BankDetailsDto BankDetails { get; set; } = new();
    }
}
