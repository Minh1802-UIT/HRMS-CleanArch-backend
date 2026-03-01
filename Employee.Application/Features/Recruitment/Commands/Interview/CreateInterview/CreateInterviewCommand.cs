using Employee.Application.Common.Security;
using Employee.Application.Features.Recruitment.Dtos;
using MediatR;

namespace Employee.Application.Features.Recruitment.Commands.Interview.CreateInterview
{
  [Authorize(Roles = "Admin,HR")]
public record CreateInterviewCommand(InterviewDto Dto) : IRequest;
}
