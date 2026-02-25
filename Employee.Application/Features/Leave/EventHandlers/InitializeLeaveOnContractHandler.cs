using MediatR;
using Employee.Application.Common.Interfaces.Organization.IService;
using Employee.Application.Features.HumanResource.Events;

namespace Employee.Application.Features.Leave.EventHandlers
{
    public class InitializeLeaveOnContractHandler : INotificationHandler<ContractCreatedEvent>
    {
        private readonly ILeaveAllocationService _leaveAllocationService;

        public InitializeLeaveOnContractHandler(ILeaveAllocationService leaveAllocationService)
        {
            _leaveAllocationService = leaveAllocationService;
        }

        public async Task Handle(ContractCreatedEvent notification, CancellationToken cancellationToken)
        {
            var currentYear = DateTime.UtcNow.Year.ToString();
            
            // Tự động khởi tạo số dư ngày phép cho nhân viên khi hợp đồng được tạo
            await _leaveAllocationService.InitializeAllocationAsync(notification.EmployeeId, currentYear);
        }
    }
}
