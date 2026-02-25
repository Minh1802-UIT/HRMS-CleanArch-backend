using Employee.Application.Features.Recruitment.Dtos;
using MediatR;

namespace Employee.Application.Features.Recruitment.Queries.Interview.GetInterviewById
{
  public record GetInterviewByIdQuery(string Id) : IRequest<InterviewResponseDto>;
}
