using Employee.Application.Common.Exceptions;
using Employee.Application.Common.Interfaces.Organization.IRepository;
using Employee.Application.Features.Recruitment.Dtos;
using Employee.Application.Features.Recruitment.Mappers;
using MediatR;

namespace Employee.Application.Features.Recruitment.Queries.Interview.GetInterviewById
{
  public class GetInterviewByIdQueryHandler : IRequestHandler<GetInterviewByIdQuery, InterviewResponseDto>
  {
    private readonly IInterviewRepository _repo;

    public GetInterviewByIdQueryHandler(IInterviewRepository repo)
    {
      _repo = repo;
    }

    public async Task<InterviewResponseDto> Handle(GetInterviewByIdQuery request, CancellationToken cancellationToken)
    {
      var entity = await _repo.GetByIdAsync(request.Id, cancellationToken)
          ?? throw new NotFoundException($"Interview with ID {request.Id} not found.");
      return entity.ToDto();
    }
  }
}
