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
using Employee.Application.Common.Exceptions;
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

        private readonly ContractService _service;

        public ContractServiceTests()
        {
            _mockRepo = new Mock<IContractRepository>();
            _mockEmpRepo = new Mock<IEmployeeRepository>();
            _mockAudit = new Mock<IAuditLogService>();
            _mockUser = new Mock<ICurrentUser>();
            _mockPublisher = new Mock<IPublisher>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();

            _service = new ContractService(
                _mockRepo.Object,
                _mockEmpRepo.Object,
                _mockAudit.Object,
                _mockUser.Object,
                _mockPublisher.Object,
                _mockUnitOfWork.Object,
                new Moq.Mock<Employee.Domain.Interfaces.Common.IDateTimeProvider>().Object
            );
        }

        [Fact]
        public async Task CreateAsync_ShouldExpireOldContract_WhenNewStartsLater()
        {
            // Arrange
            var empId = "emp1";
            var oldContract = new ContractEntity(empId, "OLD-001", new DateTime(2025, 1, 1));
            oldContract.SetId("old1");
            oldContract.Activate();

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
            _mockRepo.Setup(x => x.GetByEmployeeIdAsync(empId, It.IsAny<CancellationToken>())).ReturnsAsync(new List<ContractEntity> { oldContract });
            _mockRepo.Setup(x => x.ExistsOverlapAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime?>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            await _service.CreateAsync(dto);

            // Assert
            Assert.Equal(ContractStatus.Expired, oldContract.Status);
            Assert.Equal(dto.StartDate.AddDays(-1), oldContract.EndDate);

            _mockRepo.Verify(x => x.UpdateAsync(oldContract.Id, oldContract, It.IsAny<CancellationToken>()), Times.Once);
            _mockRepo.Verify(x => x.CreateAsync(It.IsAny<ContractEntity>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(), Times.Once);
        }

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
            _mockRepo.Setup(x => x.GetByEmployeeIdAsync(empId, It.IsAny<CancellationToken>())).ReturnsAsync(new List<ContractEntity>());

            // Simulate overlap
            _mockRepo.Setup(x => x.ExistsOverlapAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime?>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto));
            _mockUnitOfWork.Verify(x => x.RollbackTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ShouldPublishEvent()
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
            _mockRepo.Setup(x => x.GetByEmployeeIdAsync(empId, It.IsAny<CancellationToken>())).ReturnsAsync(new List<ContractEntity>());

            // Act
            await _service.CreateAsync(dto);

            // Assert
            _mockPublisher.Verify(x => x.Publish(It.IsAny<ContractCreatedEvent>(), It.IsAny<System.Threading.CancellationToken>()), Times.Once);
        }
    }
}

