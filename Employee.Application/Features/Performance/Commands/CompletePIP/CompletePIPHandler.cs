using Employee.Domain.Interfaces.Repositories;
using Employee.Application.Features.Performance.Dtos;
using FluentValidation;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Employee.Application.Features.Performance.Commands.CompletePIP
{
  public class CompletePIPCommandValidator : AbstractValidator<CompletePIPCommand>
  {
    public CompletePIPCommandValidator()
    {
      RuleFor(x => x.Id).NotEmpty().WithMessage("PIP ID is required.");
    }
  }

  public class CompletePIPHandler : IRequestHandler<CompletePIPCommand, bool>
  {
    private readonly IPIPRepository _repo;

    public CompletePIPHandler(IPIPRepository repo)
    {
      _repo = repo;
    }

    public async Task<bool> Handle(CompletePIPCommand request, CancellationToken cancellationToken)
    {
      var pip = await _repo.GetByIdAsync(request.Id, cancellationToken);
      if (pip == null) return false;

      if (request.Dto.Successful)
        pip.Complete(request.Dto.OutcomeNotes ?? string.Empty);
      else
        pip.Fail(request.Dto.OutcomeNotes ?? string.Empty);

      await _repo.UpdateAsync(pip.Id, pip, cancellationToken);
      return true;
    }
  }
}
