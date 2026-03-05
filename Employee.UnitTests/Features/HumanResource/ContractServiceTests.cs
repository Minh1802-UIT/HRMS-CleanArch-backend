using Xunit;
using Moq;
using Employee.Application.Features.HumanResource.Services;
using Employee.Domain.Interfaces.Repositories;
using Employee.Application.Common.Interfaces.Organization.IService;
using Employee.Application.Common.Interfaces;
using Employee.Domain.Entities.HumanResource;
using Employee.Domain.Common.Models;
using Employee.Application.Features.HumanResource.Dtos;
using Employee.Domain.Events;
using Employee.Application.Common.Models;
using Employee.Application.Common.Exceptions;
using Employee.Domain.Interfaces.Common;
using MediatR;
using Employee.Domain.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Threading;

namespace Employee.UnitTests.Features.HumanResource
{
    public class ContractServiceTests
    {
        private readonly Mock<IContractRepository> _mockRepo;
        private readonly Mock<IEmployeeRepository> _mockEmpRepo;
        private readonly Mock<IAuditLogService> _mockAudit;
        private readonly Mock<ICurrentUser> _mockUser;
        private readonly Mock<IPublisher> _mockPublisher;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IDateTimeProvider> _mockDateTime;

        private readonly ContractService _service;

        public ContractServiceTests()
        {
            _mockRepo = new Mock<IContractRepository>();
            _mockEmpRepo = new Mock<IEmployeeRepository>();
            _mockAudit = new Mock<IAuditLogService>();
            _mockUser = new Mock<ICurrentUser>();
            _mockPublisher = new Mock<IPublisher>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockDateTime = new Mock<IDateTimeProvider>();

            // Default: "today" is 2025-06-01 — a fixed anchor so tests are deterministic.
            _mockDateTime.Setup(x => x.UtcNow).Returns(new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc));

            _service = new ContractService(
                _mockRepo.Object,
                _mockEmpRepo.Object,
                _mockAudit.Object,
                _mockUser.Object,
                _mockPublisher.Object,
                _mockUnitOfWork.Object,
                _mockDateTime.Object
            );
        }

        // ─── Helper ──────────────────────────────────────────────────────────

        private (EmployeeEntity emp, ContractEntity old) SetupEmployeeWithActiveContract(
            string empId, DateTime oldStartDate, DateTime newStartDate)
        {
            var employee = new EmployeeEntity("EMP001", "Test", "test@hrm.com");
            employee.SetId(empId);

            var oldContract = new ContractEntity(empId, "OLD-001", oldStartDate);
            oldContract.SetId("old1");
            oldContract.Activate();

            _mockEmpRepo.Setup(x => x.GetByIdAsync(empId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(employee);
            _mockRepo.Setup(x => x.GetByEmployeeIdAsync(empId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ContractEntity> { oldContract });
            _mockRepo.Setup(x => x.ExistsOverlapAsync(
                    It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime?>(),
                    It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            return (employee, oldContract);
        }

        // ─── StartDate = today → activateNow = true ──────────────────────────

        [Fact]
        public async Task CreateAsync_WhenStartDateIsToday_ShouldActivateImmediatelyAndExpireOldContract()
        {
            // Arrange — today = 2025-06-01 (from default mock), StartDate = today
            var empId = "emp1";
            var today = new DateTime(2025, 6, 1);
            var (_, oldContract) = SetupEmployeeWithActiveContract(empId,
                oldStartDate: new DateTime(2024, 1, 1),
                newStartDate: today);

            var dto = new CreateContractDto
            {
                EmployeeId = empId,
                ContractCode = "NEW-001",
                StartDate = today,
                Salary = new SalaryInfoInputDto { BasicSalary = 1000 }
            };

            // Act
            var result = await _service.CreateAsync(dto);

            // Assert — new contract is Active immediately (ContractDto.Status is a string)
            Assert.Equal("Active", result.Status);

            // Old contract was expired atomically
            Assert.Equal(ContractStatus.Expired, oldContract.Status);
            Assert.Equal(today.AddDays(-1), oldContract.EndDate);

            _mockRepo.Verify(x => x.UpdateAsync(oldContract.Id, oldContract, It.IsAny<CancellationToken>()), Times.Once);
            _mockRepo.Verify(x => x.CreateAsync(It.IsAny<ContractEntity>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(), Times.Once);
        }

        // ─── StartDate in future → activateNow = false ───────────────────────

        [Fact]
        public async Task CreateAsync_WhenStartDateIsInFuture_ShouldSchedulePendingAndNotExpireOldContract()
        {
            // Arrange — today = 2025-06-01, StartDate = 2026-01-01 (future)
            var empId = "emp1";
            var (_, oldContract) = SetupEmployeeWithActiveContract(empId,
                oldStartDate: new DateTime(2024, 1, 1),
                newStartDate: new DateTime(2026, 1, 1));

            var dto = new CreateContractDto
            {
                EmployeeId = empId,
                ContractCode = "NEW-001",
                StartDate = new DateTime(2026, 1, 1),
                Salary = new SalaryInfoInputDto { BasicSalary = 1000 }
            };

            // Act
            var result = await _service.CreateAsync(dto);

            // Assert — new contract is Pending (background job activates on StartDate)
            Assert.Equal("Pending", result.Status);

            // Old contract should NOT be expired yet — background job handles the atomic swap
            Assert.Equal(ContractStatus.Active, oldContract.Status);

            _mockRepo.Verify(x => x.UpdateAsync(oldContract.Id, oldContract, It.IsAny<CancellationToken>()), Times.Never);
            _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(), Times.Once);
        }

        // ─── Overlap validation ───────────────────────────────────────────────

        [Fact]
        public async Task CreateAsync_ShouldThrow_WhenOverlapExists()
        {
            // Arrange
            var empId = "emp1";
            var dto = new CreateContractDto
            {
                EmployeeId = empId,
                ContractCode = "NEW-001",
                StartDate = new DateTime(2026, 1, 1),
                Salary = new SalaryInfoInputDto { BasicSalary = 1000 }
            };

            var employee = new EmployeeEntity("EMP001", "Test", "test@hrm.com");
            employee.SetId(empId);
            _mockEmpRepo.Setup(x => x.GetByIdAsync(empId, It.IsAny<CancellationToken>())).ReturnsAsync(employee);
            _mockRepo.Setup(x => x.GetByEmployeeIdAsync(empId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ContractEntity>());
            _mockRepo.Setup(x => x.ExistsOverlapAsync(
                    It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime?>(),
                    It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto));
            _mockUnitOfWork.Verify(x => x.RollbackTransactionAsync(), Times.Once);
        }

        // ─── Domain event ─────────────────────────────────────────────────────

        [Fact]
        public async Task CreateAsync_ShouldPublishContractCreatedEvent()
        {
            // Arrange
            var empId = "emp1";
            var dto = new CreateContractDto
            {
                EmployeeId = empId,
                ContractCode = "NEW-001",
                StartDate = new DateTime(2026, 1, 1), // future → Pending path
                Salary = new SalaryInfoInputDto { BasicSalary = 1000 }
            };

            var employee = new EmployeeEntity("EMP001", "Test", "test@hrm.com");
            employee.SetId(empId);
            _mockEmpRepo.Setup(x => x.GetByIdAsync(empId, It.IsAny<CancellationToken>())).ReturnsAsync(employee);
            _mockRepo.Setup(x => x.GetByEmployeeIdAsync(empId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ContractEntity>());
            _mockRepo.Setup(x => x.ExistsOverlapAsync(
                    It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime?>(),
                    It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            await _service.CreateAsync(dto);

            // Assert
            _mockPublisher.Verify(
                x => x.Publish(It.IsAny<DomainEventNotification<ContractCreatedEvent>>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        // ─── Negative salary ──────────────────────────────────────────────────

        [Fact]
        public async Task CreateAsync_ShouldThrow_WhenSalaryIsNegative()
        {
            // Arrange
            var empId = "emp1";
            var dto = new CreateContractDto
            {
                EmployeeId = empId,
                ContractCode = "NEW-001",
                StartDate = new DateTime(2026, 1, 1),
                Salary = new SalaryInfoInputDto { BasicSalary = -500 }
            };

            var employee = new EmployeeEntity("EMP001", "Test", "test@hrm.com");
            employee.SetId(empId);
            _mockEmpRepo.Setup(x => x.GetByIdAsync(empId, It.IsAny<CancellationToken>())).ReturnsAsync(employee);
            _mockRepo.Setup(x => x.GetByEmployeeIdAsync(empId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ContractEntity>());

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto));
            _mockUnitOfWork.Verify(x => x.RollbackTransactionAsync(), Times.Once);
        }
    }
}

