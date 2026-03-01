using Employee.Application.Common.Security;
using Employee.Application.Features.Recruitment.Dtos;
using MediatR;

namespace Employee.Application.Features.Recruitment.Commands.OnboardCandidate
{
    [Authorize(Roles = "Admin,HR")]
public class OnboardCandidateCommand : IRequest<string>
    {
        public string CandidateId { get; set; } = string.Empty;
        public OnboardCandidateDto OnboardData { get; set; } = null!;
    }
}
