using Employee.Application.Features.Recruitment.Dtos;
using MediatR;

namespace Employee.Application.Features.Recruitment.Commands.OnboardCandidate
{
    public class OnboardCandidateCommand : IRequest<string>
    {
        public string CandidateId { get; set; } = string.Empty;
        public OnboardCandidateDto OnboardData { get; set; } = null!;
    }
}
