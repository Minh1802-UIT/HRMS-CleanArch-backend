using Employee.Domain.Interfaces.Repositories;
using Employee.Application.Features.Performance.Dtos;
using FluentValidation;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Employee.Application.Features.Performance.Commands.UpdatePIPProgress
{
  public class UpdatePIPProgressCommandValidator : AbstractValidator<UpdatePIPProgressCommand>
  {
    public UpdatePIPProgressCommandValidator()
    {
      RuleFor(x => x.Id).NotEmpty().WithMessage("PIP ID is required.");
      RuleFor(x => x.Dto.ObjectiveIndex).GreaterThanOrEqualTo(0).WithMessage("Invalid objective index.");
      RuleFor(x => x.Dto.Progress).InclusiveBetween(0.0, 100.0).WithMessage("Progress must be between 0 and 100.");
    }
  }

  public class UpdatePIPProgressHandler : IRequestHandler<UpdatePIPProgressCommand, bool>
  {
    private readonly IPIPRepository _repo;

    public UpdatePIPProgressHandler(IPIPRepository repo)
    {
      _repo = repo;
    }

    public async Task<bool> Handle(UpdatePIPProgressCommand request, CancellationToken cancellationToken)
    {
      var pip = await _repo.GetByIdAsync(request.Id, cancellationToken);
      if (pip == null) return false;

      pip.UpdateProgress(request.Dto.ObjectiveIndex, request.Dto.Progress);
      await _repo.UpdateAsync(pip.Id, pip, cancellationToken);
      return true;
    }
  }
}
