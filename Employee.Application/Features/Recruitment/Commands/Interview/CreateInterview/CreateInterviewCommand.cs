using Employee.Application.Features.Recruitment.Dtos;
using MediatR;

namespace Employee.Application.Features.Recruitment.Commands.Interview.CreateInterview
{
  public record CreateInterviewCommand(InterviewDto Dto) : IRequest;
}
