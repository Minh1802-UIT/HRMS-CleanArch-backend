using FluentValidation;

namespace Employee.Application.Features.HumanResource.Commands.CreateEmployee
{
    public class CreateEmployeeValidator : AbstractValidator<CreateEmployeeCommand>
    {
        public CreateEmployeeValidator()
        {
            RuleFor(x => x.EmployeeCode)
                .NotEmpty().WithMessage("Employee Code is required.")
                .MaximumLength(20).WithMessage("Employee Code must not exceed 20 characters.")
                .Matches(@"^[A-Z0-9-_]+$").WithMessage("Employee Code can only contain uppercase letters, numbers, hyphens, and underscores.");

            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Full Name is required.")
                .MaximumLength(100);

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Invalid email format.");

            // Validate Sub-Objects
            RuleFor(x => x.PersonalInfo.PhoneNumber)
                .NotEmpty().WithMessage("Phone number is required.")
                .Matches(@"^0[3-9]\d{8,9}$").WithMessage("Phone number must be a valid Vietnamese format (10-11 digits, starting with 03-09).");

            RuleFor(x => x.PersonalInfo.DateOfBirth)
                 .NotEmpty()
                 .Must(BeAtLeast18YearsOld).WithMessage("Employee must be at least 18 years old.");

            RuleFor(x => x.JobDetails.DepartmentId).NotEmpty().WithMessage("Department ID is required.");
            RuleFor(x => x.JobDetails.PositionId).NotEmpty().WithMessage("Position ID is required.");
            RuleFor(x => x.JobDetails.JoinDate).NotEmpty();
        }

        // Custom Validator cho tuổi
        private bool BeAtLeast18YearsOld(DateTime dob)
        {
            var today = DateTime.Today;
            var age = today.Year - dob.Year;
            if (dob.Date > today.AddYears(-age)) age--;
            return age >= 18;
        }
    }
}
