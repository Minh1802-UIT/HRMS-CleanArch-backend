using MediatR;
using Employee.Application.Common.Interfaces.Organization.IRepository;
using Employee.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Employee.Application.Features.HumanResource.Commands.Contracts
{
    public class ExpireContractsCommand : IRequest<int>
    {
    }

    public class ExpireContractsHandler : IRequestHandler<ExpireContractsCommand, int>
    {
        private readonly IContractRepository _contractRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ExpireContractsHandler> _logger;

        public ExpireContractsHandler(
            IContractRepository contractRepo,
            IUnitOfWork unitOfWork,
            ILogger<ExpireContractsHandler> logger)
        {
            _contractRepo = contractRepo;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<int> Handle(ExpireContractsCommand request, CancellationToken cancellationToken)
        {
            var expiredContracts = await _contractRepo.GetExpiredActiveContractsAsync(DateTime.UtcNow, cancellationToken);

            if (!expiredContracts.Any())
            {
                return 0;
            }

            _logger.LogInformation("Processing {Count} expired contracts.", expiredContracts.Count);

            foreach (var contract in expiredContracts)
            {
                contract.Expire(DateTime.UtcNow);
                await _contractRepo.UpdateAsync(contract.Id, contract, cancellationToken);
            }

            _logger.LogInformation("Successfully expired {Count} contracts.", expiredContracts.Count);
            return expiredContracts.Count;
        }
    }
}
