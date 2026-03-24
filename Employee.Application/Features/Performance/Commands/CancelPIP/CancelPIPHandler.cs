using Employee.Domain.Interfaces.Repositories;
using FluentValidation;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Employee.Application.Features.Performance.Commands.CancelPIP
{
  public class CancelPIPCommandValidator : AbstractValidator<CancelPIPCommand>
  {
    public CancelPIPCommandValidator()
    {
      RuleFor(x => x.Id).NotEmpty().WithMessage("PIP ID is required.");
      RuleFor(x => x.Reason).NotEmpty().WithMessage("Cancellation reason is required.").MaximumLength(500);
    }
  }

  public class CancelPIPHandler : IRequestHandler<CancelPIPCommand, bool>
  {
    private readonly IPIPRepository _repo;

    public CancelPIPHandler(IPIPRepository repo)
    {
      _repo = repo;
    }

    public async Task<bool> Handle(CancelPIPCommand request, CancellationToken cancellationToken)
    {
      var pip = await _repo.GetByIdAsync(request.Id, cancellationToken);
      if (pip == null) return false;

      pip.Cancel(request.Reason);
      await _repo.UpdateAsync(pip.Id, pip, cancellationToken);
      return true;
    }
  }
}
