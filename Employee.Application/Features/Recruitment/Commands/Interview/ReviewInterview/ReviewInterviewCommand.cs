using Employee.Application.Features.Recruitment.Dtos;
using MediatR;

namespace Employee.Application.Features.Recruitment.Commands.Interview.ReviewInterview
{
  public record ReviewInterviewCommand(string Id, ReviewInterviewDto Dto) : IRequest;
}
