using Employee.Application.Common.Models;
using Employee.Domain.Interfaces.Repositories;
using Employee.Application.Common.Interfaces.Organization.IService;
using Employee.Application.Common.Exceptions;
using Employee.Application.Features.HumanResource.Dtos;
using Employee.Application.Features.HumanResource.Mappers;
using Employee.Domain.Entities.HumanResource;
using Employee.Domain.Events;
using MediatR;
using Employee.Domain.Entities.ValueObjects;
using Employee.Application.Common.Interfaces;
using Employee.Domain.Common.Models;
using Employee.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Employee.Application.Features.HumanResource.Services
{
    public class ContractService : IContractService
    {
        private readonly IContractRepository _repo;
        private readonly IEmployeeRepository _empRepo;
        private readonly IAuditLogService _auditService;
        private readonly ICurrentUser _currentUser;
        private readonly IPublisher _publisher;
        private readonly IUnitOfWork _unitOfWork;
        private readonly Employee.Domain.Interfaces.Common.IDateTimeProvider _dateTime;

        public ContractService(
            IContractRepository repo,
            IEmployeeRepository empRepo,
            IAuditLogService audit,
            ICurrentUser currentUser,
            IPublisher publisher,
            IUnitOfWork unitOfWork,
            Employee.Domain.Interfaces.Common.IDateTimeProvider dateTime)
        {
            _repo = repo;
            _empRepo = empRepo;
            _auditService = audit;
            _currentUser = currentUser;
            _publisher = publisher;
            _unitOfWork = unitOfWork;
            _dateTime = dateTime;
        }

        public async Task<PagedResult<ContractDto>> GetPagedAsync(PaginationParams pagination)
        {
            var pagedEntity = await _repo.GetPagedAsync(pagination);
            return new PagedResult<ContractDto>
            {
                Items = pagedEntity.Items.Select(c => c.ToDto()).ToList(),
                TotalCount = pagedEntity.TotalCount,
                PageNumber = pagedEntity.PageNumber,
                PageSize = pagedEntity.PageSize
            };
        }

        public async Task<ContractDto> GetByIdAsync(string id)
        {
            var contract = await _repo.GetByIdAsync(id);
            if (contract == null) throw new NotFoundException($"Contract with ID {id} not found.");
            return contract.ToDto();
        }

        public async Task<IEnumerable<ContractDto>> GetByEmployeeIdAsync(string empId)
        {
            var list = await _repo.GetByEmployeeIdAsync(empId);
            return list.Select(c => c.ToDto());
        }

        public async Task<ContractDto> CreateAsync(CreateContractDto dto)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var emp = await _empRepo.GetByIdAsync(dto.EmployeeId);
                if (emp == null) throw new NotFoundException($"Employee with ID '{dto.EmployeeId}' not found.");

                if (dto.EndDate.HasValue && dto.EndDate < dto.StartDate)
                    throw new ValidationException("End date cannot be before start date.");

                if (dto.Salary.BasicSalary < 0)
                    throw new ValidationException("Basic salary cannot be negative.");

                var activeContracts = (await _repo.GetByEmployeeIdAsync(dto.EmployeeId))
                    .Where(c => c.Status == ContractStatus.Active).ToList();

                var contractsToExpire = new List<ContractEntity>();
                var excludedIds = new List<string>();

                foreach (var oldC in activeContracts)
                {
                    if (dto.StartDate > oldC.StartDate)
                    {
                        contractsToExpire.Add(oldC);
                        excludedIds.Add(oldC.Id);
                    }
                }

                var isOverlap = await _repo.ExistsOverlapAsync(dto.EmployeeId, dto.StartDate, dto.EndDate, excludedIds);
                if (isOverlap)
                {
                    throw new ValidationException("New contract overlaps with an existing active contract.");
                }

                foreach (var oldC in contractsToExpire)
                {
                    oldC.Expire(dto.StartDate.AddDays(-1));
                    await _repo.UpdateAsync(oldC.Id, oldC);
                }

                var contract = dto.ToEntity();
                contract.Activate();

                await _repo.CreateAsync(contract);

                await _publisher.Publish(new DomainEventNotification<ContractCreatedEvent>(new ContractCreatedEvent(contract.EmployeeId, contract.Id)));

                await _unitOfWork.CommitTransactionAsync();

                return contract.ToDto();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task UpdateAsync(string id, UpdateContractDto dto)
        {
            var contract = await _repo.GetByIdAsync(id)
                ?? throw new NotFoundException($"Contract with ID '{id}' not found.");

            if (dto.EndDate != contract.EndDate)
            {
                var isOverlap = await _repo.ExistsOverlapAsync(contract.EmployeeId, contract.StartDate, dto.EndDate, new List<string> { id });
                if (isOverlap)
                {
                    throw new ValidationException("Contract time overlaps with another contract.");
                }
            }

            contract.UpdateDates(contract.StartDate, dto.EndDate);

            if (dto.Salary != null)
            {
                var salary = new SalaryComponents
                {
                    BasicSalary = dto.Salary.BasicSalary,
                    TransportAllowance = dto.Salary.TransportAllowance,
                    LunchAllowance = dto.Salary.LunchAllowance,
                    OtherAllowance = dto.Salary.OtherAllowance
                };
                contract.UpdateSalary(salary);
            }

            await _repo.UpdateAsync(id, contract);

            await _auditService.LogAsync(
                userId: _currentUser.UserId,
                userName: _currentUser.UserName ?? "Unknown",
                action: "UPDATE_CONTRACT",
                tableName: "Contracts",
                recordId: id,
                oldVal: new { Note = "Audit update" },
                newVal: new { EndDate = dto.EndDate, BasicSalary = dto.Salary?.BasicSalary }
            );
        }

        public async Task TerminateAsync(string id)
        {
            var contract = await _repo.GetByIdAsync(id)
                ?? throw new NotFoundException($"Contract with ID '{id}' not found.");

            if (contract.Status == ContractStatus.Terminated)
                throw new ValidationException("This contract is already terminated.");

            contract.Terminate("Manual termination", _dateTime.UtcNow);

            await _repo.UpdateAsync(id, contract);

            await _auditService.LogAsync(
                userId: _currentUser.UserId,
                userName: _currentUser.UserName ?? "Unknown",
                action: "TERMINATE_CONTRACT",
                tableName: "Contracts",
                recordId: id,
                oldVal: new { Status = "Active" },
                newVal: new { Status = "Terminated" }
            );
        }

        public async Task DeleteAsync(string id) => await _repo.DeleteAsync(id);
    }
}


