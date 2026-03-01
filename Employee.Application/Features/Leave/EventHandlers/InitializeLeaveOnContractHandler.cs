using Employee.Application.Common.Models;
using MediatR;
using Employee.Application.Common.Interfaces.Organization.IService;
using Employee.Domain.Events;

namespace Employee.Application.Features.Leave.EventHandlers
{
    public class InitializeLeaveOnContractHandler : INotificationHandler<DomainEventNotification<ContractCreatedEvent>>
    {
        private readonly ILeaveAllocationService _leaveAllocationService;

        public InitializeLeaveOnContractHandler(ILeaveAllocationService leaveAllocationService)
        {
            _leaveAllocationService = leaveAllocationService;
        }

        public async Task Handle(DomainEventNotification<ContractCreatedEvent> notificationWrapper, CancellationToken cancellationToken)
        {
            var currentYear = DateTime.UtcNow.Year.ToString();
            
            // Tự động khởi tạo số dư ngày phép cho nhân viên khi hợp đồng được tạo
            await _leaveAllocationService.InitializeAllocationAsync(notificationWrapper.DomainEvent.EmployeeId, currentYear);
        }
    }
}

