using Employee.Domain.Interfaces.Repositories;
using Employee.Application.Features.Recruitment.Mappers;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Employee.Application.Features.Recruitment.Commands.Candidate.CreateCandidate
{
    public class CreateCandidateHandler : IRequestHandler<CreateCandidateCommand>
    {
        private readonly ICandidateRepository _repo;
        private readonly Employee.Domain.Interfaces.Common.IDateTimeProvider _dateTime;

        public CreateCandidateHandler(ICandidateRepository repo, Employee.Domain.Interfaces.Common.IDateTimeProvider dateTime)
        {
            _repo = repo;
            _dateTime = dateTime;
        }

        public async Task Handle(CreateCandidateCommand request, CancellationToken cancellationToken)
        {
            var entity = request.Dto.ToEntity(_dateTime.UtcNow);
            await _repo.CreateAsync(entity, cancellationToken);
        }
    }
}
