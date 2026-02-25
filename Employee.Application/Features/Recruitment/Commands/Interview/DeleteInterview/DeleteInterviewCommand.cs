using MediatR;

namespace Employee.Application.Features.Recruitment.Commands.Interview.DeleteInterview
{
  public record DeleteInterviewCommand(string Id) : IRequest;
}
