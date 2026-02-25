using FluentValidation;

namespace Employee.Application.Features.HumanResource.Commands.UpdateEmployee
{
    public class UpdateEmployeeValidator : AbstractValidator<UpdateEmployeeCommand>
    {
        public UpdateEmployeeValidator()
        {
            RuleFor(x => x.Id).NotEmpty();

            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Full Name is required.")
                .MaximumLength(100);

            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("Invalid email format.")
                .When(x => !string.IsNullOrEmpty(x.Email));

            // Validate nested
            RuleFor(x => x.JobDetails.DepartmentId).NotEmpty().WithMessage("Department ID is required.");
            RuleFor(x => x.JobDetails.PositionId).NotEmpty().WithMessage("Position ID is required.");
        }
    }
}
