using Employee.Application.Features.Recruitment.Dtos;
using MediatR;

namespace Employee.Application.Features.Recruitment.Commands.Interview.UpdateInterview
{
  public record UpdateInterviewCommand(string Id, InterviewDto Dto) : IRequest;
}
