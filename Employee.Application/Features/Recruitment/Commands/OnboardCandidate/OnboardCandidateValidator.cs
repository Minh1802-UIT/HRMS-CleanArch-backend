using FluentValidation;

namespace Employee.Application.Features.Recruitment.Commands.OnboardCandidate
{
    public class OnboardCandidateValidator : AbstractValidator<OnboardCandidateCommand>
    {
        public OnboardCandidateValidator()
        {
            RuleFor(x => x.CandidateId).NotEmpty().WithMessage("CandidateId không được để trống.");
            RuleFor(x => x.OnboardData.EmployeeCode).NotEmpty().WithMessage("Mã nhân viên không được để trống.");
            RuleFor(x => x.OnboardData.DepartmentId).NotEmpty().WithMessage("Phòng ban không được để trống.");
            RuleFor(x => x.OnboardData.PositionId).NotEmpty().WithMessage("Chức vụ không được để trống.");
            RuleFor(x => x.OnboardData.JoinDate).NotEmpty().WithMessage("Ngày vào làm không được để trống.");
        }
    }
}
