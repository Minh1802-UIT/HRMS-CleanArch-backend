using Employee.Application.Common.Exceptions;
using Employee.Domain.Interfaces.Repositories;
using Employee.Application.Features.Recruitment.Dtos;
using Employee.Application.Features.Recruitment.Mappers;
using MediatR;

namespace Employee.Application.Features.Recruitment.Queries.Candidate.GetCandidateById
{
  public class GetCandidateByIdQueryHandler : IRequestHandler<GetCandidateByIdQuery, CandidateResponseDto>
  {
    private readonly ICandidateRepository _repo;

    public GetCandidateByIdQueryHandler(ICandidateRepository repo)
    {
      _repo = repo;
    }

    public async Task<CandidateResponseDto> Handle(GetCandidateByIdQuery request, CancellationToken cancellationToken)
    {
      var entity = await _repo.GetByIdAsync(request.Id, cancellationToken)
          ?? throw new NotFoundException($"Candidate with ID {request.Id} not found.");
      return entity.ToDto();
    }
  }
}
