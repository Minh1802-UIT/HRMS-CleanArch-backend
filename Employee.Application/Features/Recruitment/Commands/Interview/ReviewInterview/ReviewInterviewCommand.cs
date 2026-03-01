using Employee.Application.Common.Security;
using Employee.Application.Features.Recruitment.Dtos;
using MediatR;

namespace Employee.Application.Features.Recruitment.Commands.Interview.ReviewInterview
{
  [Authorize(Roles = "Admin,HR")]
public record ReviewInterviewCommand(string Id, ReviewInterviewDto Dto) : IRequest;
}
