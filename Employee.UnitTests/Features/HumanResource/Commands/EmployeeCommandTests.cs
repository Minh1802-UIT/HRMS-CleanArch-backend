using Xunit;
using Moq;
using Employee.Application.Features.HumanResource.Commands.CreateEmployee;
using Employee.Application.Features.HumanResource.Commands.UpdateEmployee;
using Employee.Domain.Interfaces.Repositories;
using Employee.Application.Common.Interfaces.Organization.IService;
using Employee.Application.Common.Interfaces;
using Employee.Domain.Entities.HumanResource;
using Employee.Domain.Entities.Organization;
using Employee.Application.Features.Organization.Dtos;
using Employee.Domain.Events;
using Employee.Application.Common.Models;
using Employee.Application.Common.Exceptions;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using System;
using Employee.Domain.Entities.ValueObjects;
using Employee.Domain.Common.Models;
using Employee.Application.Features.HumanResource.Dtos;

namespace Employee.UnitTests.Features.HumanResource.Commands
{
  public class EmployeeCommandTests
  {
    private readonly Mock<IEmployeeRepository> _mockRepo;
    private readonly Mock<IDepartmentRepository> _mockDeptRepo;
    private readonly Mock<IPositionRepository> _mockPosRepo;
    private readonly Mock<IPublisher> _mockPublisher;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;

    private readonly CreateEmployeeHandler _createHandler;
    private readonly UpdateEmployeeHandler _updateHandler;

    public EmployeeCommandTests()
    {
      _mockRepo = new Mock<IEmployeeRepository>();
      _mockDeptRepo = new Mock<IDepartmentRepository>();
      _mockPosRepo = new Mock<IPositionRepository>();
      _mockPublisher = new Mock<IPublisher>();
      _mockUnitOfWork = new Mock<IUnitOfWork>();

      _createHandler = new CreateEmployeeHandler(
          _mockRepo.Object, _mockDeptRepo.Object, _mockPosRepo.Object,
          _mockPublisher.Object, _mockUnitOfWork.Object);

      _updateHandler = new UpdateEmployeeHandler(
          _mockRepo.Object, _mockDeptRepo.Object, _mockPosRepo.Object,
          _mockPublisher.Object);
    }

    // --- Create Employee Tests ---

    [Fact]
    public async Task Create_Valid_ShouldCreateAndPublishEvent()
    {
      // Arrange
      var command = new CreateEmployeeCommand
      {
        EmployeeCode = "E001",
        FullName = "Test User",
        Email = "test@example.com",
        PersonalInfo = new PersonalInfoDto
        {
          DateOfBirth = DateTime.UtcNow.AddYears(-20),
          IdentityCard = "123456789",
          PhoneNumber = "0901234567",
          Gender = "Male"
        },
        JobDetails = new JobDetailsDto
        {
          DepartmentId = "D1",
          PositionId = "P1",
          JoinDate = DateTime.UtcNow
        },
        BankDetails = new BankDetailsDto
        {
          BankName = "Test Bank",
          AccountNumber = "123456"
        }
      };

      _mockRepo.Setup(x => x.ExistsByCodeAsync(command.EmployeeCode, It.IsAny<CancellationToken>())).ReturnsAsync(false);
      _mockDeptRepo.Setup(x => x.GetByIdAsync("D1", It.IsAny<CancellationToken>())).ReturnsAsync(new Department("IT", "IT"));
      _mockPosRepo.Setup(x => x.GetByIdAsync("P1", It.IsAny<CancellationToken>())).ReturnsAsync(new Position("Dev", "DEV", "D1"));

      // Act
      await _createHandler.Handle(command, CancellationToken.None);

      // Assert
      _mockRepo.Verify(x => x.CreateAsync(It.Is<EmployeeEntity>(e => e.EmployeeCode == "E001"), It.IsAny<CancellationToken>()), Times.Once);
      _mockPublisher.Verify(x => x.Publish(It.IsAny<DomainEventNotification<EmployeeCreatedEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
      _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(), Times.Once);
    }

    [Fact]
    public async Task Create_Underage_ShouldThrow()
    {
      // Arrange
      var command = new CreateEmployeeCommand
      {
        EmployeeCode = "E001",
        FullName = "Underage User",
        Email = "underage@test.com",
        PersonalInfo = new PersonalInfoDto
        {
          DateOfBirth = DateTime.UtcNow.AddYears(-17),
          IdentityCard = "123",
          PhoneNumber = "123",
          Gender = "Male"
        },
        JobDetails = new JobDetailsDto { JoinDate = DateTime.UtcNow, DepartmentId = "D1", PositionId = "P1" }
      };
      _mockRepo.Setup(x => x.ExistsByCodeAsync(command.EmployeeCode, It.IsAny<CancellationToken>())).ReturnsAsync(false);
      _mockDeptRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new Department("IT", "IT"));
      _mockPosRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new Position("Dev", "DEV", "D1"));

      // Act & Assert
      // Note: Age validation now throws ArgumentException in Domain Entity
      var ex = await Assert.ThrowsAsync<ArgumentException>(() => _createHandler.Handle(command, CancellationToken.None));
      Assert.Contains("18 years old", ex.Message);
    }

    // --- Update Employee Tests ---

    [Fact]
    public async Task Update_Valid_ShouldUpdateAndPublish()
    {
      // Arrange
      var empId = "E1";
      var version = 1;
      // Existing entity uses Domain Value Object
      var existingEmp = new EmployeeEntity("E1", "Existing", "ex@test.com");
      existingEmp.SetId(empId);
      existingEmp.SetVersion(version);
      existingEmp.UpdateJobDetails(new JobDetails { DepartmentId = "D1", PositionId = "P1" });

      // Command uses DTOs
      var command = new UpdateEmployeeCommand
      {
        Id = empId,
        Version = version,
        FullName = "Updated Name",
        Email = "updated@test.com",
        JobDetails = new JobDetailsDto
        {
          DepartmentId = "D1",
          PositionId = "P1",
          JoinDate = DateTime.UtcNow
        },
        PersonalInfo = new PersonalInfoDto
        {
          DateOfBirth = DateTime.UtcNow.AddYears(-25),
          IdentityCard = "999",
          PhoneNumber = "999",
          Gender = "Female"
        },
        BankDetails = new BankDetailsDto
        {
          BankName = "Bank",
          AccountNumber = "111"
        }
      };

      _mockRepo.Setup(x => x.GetByIdAsync(empId, It.IsAny<CancellationToken>())).ReturnsAsync(existingEmp);

      // Act
      await _updateHandler.Handle(command, CancellationToken.None);

      // Assert
      _mockRepo.Verify(x => x.UpdateAsync(empId, existingEmp, version, It.IsAny<CancellationToken>()), Times.Once);
      _mockPublisher.Verify(x => x.Publish(It.IsAny<DomainEventNotification<EmployeeUpdatedEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_ConcurrencyMismatch_ShouldThrow()
    {
      // Arrange
      var empId = "E1";
      var existingEmp = new EmployeeEntity("E1", "Existing", "ex@test.com");
      existingEmp.SetId(empId);
      existingEmp.SetVersion(2); // DB has v2
      var command = new UpdateEmployeeCommand { Id = empId, Version = 1 }; // Request sends v1

      _mockRepo.Setup(x => x.GetByIdAsync(empId, It.IsAny<CancellationToken>())).ReturnsAsync(existingEmp);

      // Act & Assert
      await Assert.ThrowsAsync<ConcurrencyException>(() => _updateHandler.Handle(command, CancellationToken.None));
    }
  }
}
