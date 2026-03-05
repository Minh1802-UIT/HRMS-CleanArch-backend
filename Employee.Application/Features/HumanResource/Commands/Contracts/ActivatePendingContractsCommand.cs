using MediatR;
using Employee.Domain.Interfaces.Repositories;
using Employee.Application.Common.Interfaces;
using Employee.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Employee.Application.Features.HumanResource.Commands.Contracts
{
  /// <summary>
  /// Activates all Pending contracts whose StartDate &lt;= today, and atomically
  /// expires any previously-Active contract for the same employee.
  ///
  /// Run nightly BEFORE ExpireContractsCommand so that a Pending contract that
  /// starts today is Active before the expiry sweep runs.
  /// </summary>
  public class ActivatePendingContractsCommand : IRequest<int>
  {
  }

  public class ActivatePendingContractsHandler : IRequestHandler<ActivatePendingContractsCommand, int>
  {
    private readonly IContractRepository _contractRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ActivatePendingContractsHandler> _logger;

    public ActivatePendingContractsHandler(
        IContractRepository contractRepo,
        IUnitOfWork unitOfWork,
        ILogger<ActivatePendingContractsHandler> logger)
    {
      _contractRepo = contractRepo;
      _unitOfWork = unitOfWork;
      _logger = logger;
    }

    public async Task<int> Handle(ActivatePendingContractsCommand request, CancellationToken cancellationToken)
    {
      var today = DateTime.UtcNow;
      var pendingContracts = await _contractRepo.GetPendingContractsDueAsync(today, cancellationToken);

      if (!pendingContracts.Any())
        return 0;

      _logger.LogInformation("Activating {Count} pending contracts.", pendingContracts.Count);

      int activated = 0;

      foreach (var pending in pendingContracts)
      {
        await _unitOfWork.BeginTransactionAsync();
        try
        {
          // 1. Expire any currently-Active contract for this employee
          //    (those whose StartDate is earlier than the incoming contract)
          var existing = await _contractRepo.GetByEmployeeIdAsync(pending.EmployeeId, cancellationToken);
          var toExpire = existing
              .Where(c => c.Status == ContractStatus.Active && c.StartDate < pending.StartDate)
              .ToList();

          foreach (var old in toExpire)
          {
            old.Expire(pending.StartDate.AddDays(-1));
            await _contractRepo.UpdateAsync(old.Id, old, cancellationToken);
          }

          // 2. Activate the pending contract
          pending.Activate();
          await _contractRepo.UpdateAsync(pending.Id, pending, cancellationToken);

          await _unitOfWork.CommitTransactionAsync();
          activated++;

          _logger.LogInformation(
              "Contract {ContractId} for employee {EmployeeId} activated. Expired {Count} old contract(s).",
              pending.Id, pending.EmployeeId, toExpire.Count);
        }
        catch (Exception ex)
        {
          await _unitOfWork.RollbackTransactionAsync();
          _logger.LogError(ex,
              "Failed to activate pending contract {ContractId} for employee {EmployeeId}.",
              pending.Id, pending.EmployeeId);
        }
      }

      return activated;
    }
  }
}
