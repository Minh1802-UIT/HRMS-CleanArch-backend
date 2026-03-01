using FluentValidation;
using Employee.Application.Features.Recruitment.Dtos;

namespace Employee.Application.Features.Recruitment.Validators
{
    public class CandidateValidator : AbstractValidator<CandidateDto>
    {
        public CandidateValidator()
        {
            RuleFor(x => x.FullName).NotEmpty().WithMessage("Họ tên không được để trống.");
            RuleFor(x => x.Email).EmailAddress().WithMessage("Email không hợp lệ.");
            RuleFor(x => x.Phone).NotEmpty().WithMessage("Số điện thoại không được để trống.")
                .Matches(@"^0[3-9]\d{8,9}$").WithMessage("Số điện thoại phải đúng định dạng Việt Nam (10-11 số, bắt đầu bằng 03-09).");
            RuleFor(x => x.JobVacancyId).NotEmpty().WithMessage("Mã tin tuyển dụng không được để trống.");
        }
    }

    public class JobVacancyValidator : AbstractValidator<JobVacancyDto>
    {
        public JobVacancyValidator()
        {
            RuleFor(x => x.Title).NotEmpty().WithMessage("Tiêu đề không được để trống.");
            RuleFor(x => x.Vacancies).GreaterThan(0).WithMessage("Số lượng tuyển phải lớn hơn 0.");
            RuleFor(x => x.ExpiredDate).GreaterThan(DateTime.UtcNow).WithMessage("Ngày hết hạn phải ở tương lai.");
        }
    }

    public class InterviewValidator : AbstractValidator<InterviewDto>
    {
        public InterviewValidator()
        {
            RuleFor(x => x.CandidateId).NotEmpty().WithMessage("Mã ứng viên không được để trống.");
            RuleFor(x => x.InterviewerId).NotEmpty().WithMessage("Mã người phỏng vấn không được để trống.");
            RuleFor(x => x.ScheduledTime).GreaterThan(DateTime.UtcNow).WithMessage("Thời gian phỏng vấn phải ở tương lai.");
        }
    }
}
