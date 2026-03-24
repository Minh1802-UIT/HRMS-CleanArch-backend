using Employee.Domain.Interfaces.Repositories;
using Employee.Domain.Entities.Performance;
using Employee.Application.Features.Performance.Dtos;
using FluentValidation;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Employee.Application.Features.Performance.Commands.CreatePIP
{
  public class CreatePIPCommandValidator : AbstractValidator<CreatePIPCommand>
  {
    public CreatePIPCommandValidator()
    {
      RuleFor(x => x.Dto.EmployeeId).NotEmpty().WithMessage("Employee ID is required.");
      RuleFor(x => x.Dto.ManagerId).NotEmpty().WithMessage("Manager ID is required.");
      RuleFor(x => x.Dto.Title).NotEmpty().WithMessage("Title is required.").MaximumLength(200);
      RuleFor(x => x.Dto.StartDate).NotEmpty().WithMessage("Start date is required.");
      RuleFor(x => x.Dto.EndDate).NotEmpty().WithMessage("End date is required.")
          .GreaterThan(x => x.Dto.StartDate).WithMessage("End date must be after start date.");
      RuleFor(x => x.Dto.Objectives).NotNull().WithMessage("At least one objective is required.")
          .Must(o => o != null && o.Count > 0).WithMessage("At least one objective is required.");
      RuleForEach(x => x.Dto.Objectives).ChildRules(obj =>
      {
        obj.RuleFor(o => o.Description).NotEmpty().WithMessage("Objective description is required.");
      });
    }
  }

  public class CreatePIPHandler : IRequestHandler<CreatePIPCommand, string>
  {
    private readonly IPIPRepository _repo;

    public CreatePIPHandler(IPIPRepository repo)
    {
      _repo = repo;
    }

    public async Task<string> Handle(CreatePIPCommand request, CancellationToken cancellationToken)
    {
      var objectives = request.Dto.Objectives?
        .Select(o => new PIPObjective(o.Description, o.SuccessCriteria ?? string.Empty, o.TargetDate))
        .ToList() ?? new List<PIPObjective>();

      var pip = new PIP(
        request.Dto.EmployeeId,
        request.Dto.ManagerId,
        request.Dto.Title,
        request.Dto.Description ?? string.Empty,
        request.Dto.StartDate,
        request.Dto.EndDate,
        objectives
      );

      await _repo.CreateAsync(pip, cancellationToken);
      return pip.Id;
    }
  }
}
