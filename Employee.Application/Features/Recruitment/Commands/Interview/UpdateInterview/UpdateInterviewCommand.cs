using Employee.Application.Common.Security;
using Employee.Application.Features.Recruitment.Dtos;
using MediatR;

namespace Employee.Application.Features.Recruitment.Commands.Interview.UpdateInterview
{
  [Authorize(Roles = "Admin,HR")]
public record UpdateInterviewCommand(string Id, InterviewDto Dto) : IRequest;
}
