using Xunit;
using Moq;
using Employee.Application.Features.HumanResource.Commands.Contracts;
using Employee.Application.Common.Interfaces;
using Employee.Domain.Interfaces.Repositories;
using Employee.Domain.Entities.HumanResource;
using Employee.Domain.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Employee.UnitTests.Features.HumanResource.Commands
{
  public class ActivatePendingContractsHandlerTests
  {
    private readonly Mock<IContractRepository> _mockRepo;
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<ILogger<ActivatePendingContractsHandler>> _mockLogger;
    private readonly ActivatePendingContractsHandler _handler;

    public ActivatePendingContractsHandlerTests()
    {
      _mockRepo = new Mock<IContractRepository>();
      _mockUow = new Mock<IUnitOfWork>();
      _mockLogger = new Mock<ILogger<ActivatePendingContractsHandler>>();

      _handler = new ActivatePendingContractsHandler(
          _mockRepo.Object,
          _mockUow.Object,
          _mockLogger.Object);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────

    private static ContractEntity BuildPendingContract(string id, string empId, DateTime startDate)
    {
      var c = new ContractEntity(empId, $"CTR-{id}", startDate);
      c.SetId(id);
      c.ScheduleActivation(); // Draft → Pending
      return c;
    }

    private static ContractEntity BuildActiveContract(string id, string empId, DateTime startDate)
    {
      var c = new ContractEntity(empId, $"CTR-ACT-{id}", startDate);
      c.SetId(id);
      c.Activate(); // Draft → Active
      return c;
    }

    // ─── No-op scenario ──────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenNoPendingContractsDue_ShouldReturnZeroAndNotWriteAnything()
    {
      // Arrange
      _mockRepo
          .Setup(x => x.GetPendingContractsDueAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync(new List<ContractEntity>());

      // Act
      var result = await _handler.Handle(new ActivatePendingContractsCommand(), CancellationToken.None);

      // Assert
      Assert.Equal(0, result);
      _mockUow.Verify(x => x.BeginTransactionAsync(), Times.Never);
      _mockRepo.Verify(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<ContractEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─── Happy-path: one pending, one old active ─────────────────────────

    [Fact]
    public async Task Handle_WhenPendingContractDue_ShouldActivatePendingAndExpireOldActive()
    {
      // Arrange
      var empId = "emp1";
      var today = new DateTime(2026, 1, 1);
      var oldStartDate = new DateTime(2024, 1, 1);

      var pending = BuildPendingContract("pend1", empId, today);
      var oldActive = BuildActiveContract("act1", empId, oldStartDate);

      _mockRepo
          .Setup(x => x.GetPendingContractsDueAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync(new List<ContractEntity> { pending });
      _mockRepo
          .Setup(x => x.GetByEmployeeIdAsync(empId, It.IsAny<CancellationToken>()))
          .ReturnsAsync(new List<ContractEntity> { oldActive, pending });

      // Act
      var result = await _handler.Handle(new ActivatePendingContractsCommand(), CancellationToken.None);

      // Assert
      Assert.Equal(1, result);
      Assert.Equal(ContractStatus.Active, pending.Status);
      Assert.Equal(ContractStatus.Expired, oldActive.Status);
      Assert.Equal(today.AddDays(-1), oldActive.EndDate);

      // Pending + old active both updated; transaction committed once
      _mockRepo.Verify(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<ContractEntity>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
      _mockUow.Verify(x => x.CommitTransactionAsync(), Times.Once);
      _mockUow.Verify(x => x.RollbackTransactionAsync(), Times.Never);
    }

    // ─── No old active contract ───────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenPendingWithNoOldActive_ShouldActivateWithoutExpiring()
    {
      // Arrange
      var empId = "emp2";
      var today = new DateTime(2026, 1, 1);
      var pending = BuildPendingContract("pend2", empId, today);

      _mockRepo
          .Setup(x => x.GetPendingContractsDueAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync(new List<ContractEntity> { pending });
      _mockRepo
          .Setup(x => x.GetByEmployeeIdAsync(empId, It.IsAny<CancellationToken>()))
          .ReturnsAsync(new List<ContractEntity> { pending });

      // Act
      var result = await _handler.Handle(new ActivatePendingContractsCommand(), CancellationToken.None);

      // Assert
      Assert.Equal(1, result);
      Assert.Equal(ContractStatus.Active, pending.Status);

      // Only the pending contract is updated (no old active)
      _mockRepo.Verify(x => x.UpdateAsync(pending.Id, pending, It.IsAny<CancellationToken>()), Times.Once);
      _mockUow.Verify(x => x.CommitTransactionAsync(), Times.Once);
    }

    // ─── Failure: one contract fails, rolls back, count stays correct ─────

    [Fact]
    public async Task Handle_WhenActivationFails_ShouldRollbackAndNotCountContract()
    {
      // Arrange
      var empId = "emp3";
      var today = new DateTime(2026, 1, 1);
      var pending = BuildPendingContract("pend3", empId, today);

      _mockRepo
          .Setup(x => x.GetPendingContractsDueAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync(new List<ContractEntity> { pending });
      _mockRepo
          .Setup(x => x.GetByEmployeeIdAsync(empId, It.IsAny<CancellationToken>()))
          .ReturnsAsync(new List<ContractEntity> { pending });
      _mockRepo
          .Setup(x => x.UpdateAsync(pending.Id, pending, It.IsAny<CancellationToken>()))
          .ThrowsAsync(new Exception("DB failure"));

      // Act
      var result = await _handler.Handle(new ActivatePendingContractsCommand(), CancellationToken.None);

      // Assert — rolled back, count = 0, did not rethrow
      Assert.Equal(0, result);
      _mockUow.Verify(x => x.RollbackTransactionAsync(), Times.Once);
      _mockUow.Verify(x => x.CommitTransactionAsync(), Times.Never);
    }

    // ─── Multiple contracts: partial success ──────────────────────────────

    [Fact]
    public async Task Handle_WhenTwoPendingAndOneFails_ShouldReturnCountOfSuccessful()
    {
      // Arrange
      var today = new DateTime(2026, 1, 1);
      var pending1 = BuildPendingContract("pend-ok", "emp4", today);
      var pending2 = BuildPendingContract("pend-fail", "emp5", today);

      _mockRepo
          .Setup(x => x.GetPendingContractsDueAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync(new List<ContractEntity> { pending1, pending2 });

      // emp4 → no old contracts
      _mockRepo.Setup(x => x.GetByEmployeeIdAsync("emp4", It.IsAny<CancellationToken>()))
          .ReturnsAsync(new List<ContractEntity> { pending1 });

      // emp5 → no old contracts
      _mockRepo.Setup(x => x.GetByEmployeeIdAsync("emp5", It.IsAny<CancellationToken>()))
          .ReturnsAsync(new List<ContractEntity> { pending2 });

      // emp4 update succeeds; emp5 update throws
      _mockRepo.Setup(x => x.UpdateAsync(pending1.Id, pending1, It.IsAny<CancellationToken>()))
          .Returns(Task.CompletedTask);
      _mockRepo.Setup(x => x.UpdateAsync(pending2.Id, pending2, It.IsAny<CancellationToken>()))
          .ThrowsAsync(new Exception("DB failure for emp5"));

      // Act
      var result = await _handler.Handle(new ActivatePendingContractsCommand(), CancellationToken.None);

      // Assert — only pending1 counted
      Assert.Equal(1, result);
      Assert.Equal(ContractStatus.Active, pending1.Status);
      _mockUow.Verify(x => x.CommitTransactionAsync(), Times.Once);
      _mockUow.Verify(x => x.RollbackTransactionAsync(), Times.Once);
    }
  }
}
