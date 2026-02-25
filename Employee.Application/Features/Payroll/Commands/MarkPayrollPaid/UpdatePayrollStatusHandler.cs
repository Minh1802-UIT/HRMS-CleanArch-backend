using Employee.Application.Common.Exceptions;
using Employee.Application.Common.Interfaces.Organization.IRepository;
using Employee.Domain.Enums;
using MediatR;

namespace Employee.Application.Features.Payroll.Commands.MarkPayrollPaid
{
  public class UpdatePayrollStatusHandler : IRequestHandler<UpdatePayrollStatusCommand>
  {
    private readonly IPayrollRepository _repo;

    public UpdatePayrollStatusHandler(IPayrollRepository repo)
    {
      _repo = repo;
    }

    public async Task Handle(UpdatePayrollStatusCommand request, CancellationToken cancellationToken)
    {
      var entity = await _repo.GetByIdAsync(request.Id, cancellationToken);
      if (entity == null) throw new NotFoundException($"Payroll {request.Id} not found");

      switch (request.Status.ToLower())
      {
        case "approved":
          if (entity.Status == PayrollStatus.Paid)
            entity.RevertToApproved();
          else
            entity.Approve();
          break;

        case "paid":
          entity.MarkAsPaid(DateTime.UtcNow);
          break;

        case "rejected":
          entity.Reject();
          break;

        default:
          throw new ValidationException($"Invalid status: {request.Status}");
      }

      await _repo.UpdateAsync(request.Id, entity, cancellationToken);
    }
  }
}
