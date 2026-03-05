using Employee.Domain.Entities.HumanResource;
using Employee.Domain.Enums;
using System;
using Xunit;

namespace Employee.UnitTests.Domain.Entities.HumanResource
{
  public class ContractEntityTests
  {
    private static ContractEntity CreateDraftContract() =>
        new ContractEntity("emp1", "CTR-001", new DateTime(2026, 1, 1));

    // ─── ScheduleActivation ─────────────────────────────────────────────

    [Fact]
    public void ScheduleActivation_WhenDraft_ShouldTransitionToPending()
    {
      var contract = CreateDraftContract();
      contract.ScheduleActivation();
      Assert.Equal(ContractStatus.Pending, contract.Status);
    }

    [Fact]
    public void ScheduleActivation_WhenAlreadyPending_ShouldThrowInvalidOperationException()
    {
      var contract = CreateDraftContract();
      contract.ScheduleActivation(); // Draft → Pending

      Assert.Throws<InvalidOperationException>(() => contract.ScheduleActivation());
    }

    [Fact]
    public void ScheduleActivation_WhenActive_ShouldThrowInvalidOperationException()
    {
      var contract = CreateDraftContract();
      contract.Activate(); // Draft → Active

      Assert.Throws<InvalidOperationException>(() => contract.ScheduleActivation());
    }

    // ─── Activate ───────────────────────────────────────────────────────

    [Fact]
    public void Activate_WhenDraft_ShouldTransitionToActive()
    {
      var contract = CreateDraftContract();
      contract.Activate();
      Assert.Equal(ContractStatus.Active, contract.Status);
    }

    [Fact]
    public void Activate_WhenPending_ShouldTransitionToActive()
    {
      // Represents the background-job path: Draft → Pending (creation day),
      // then → Active (on StartDate via nightly job).
      var contract = CreateDraftContract();
      contract.ScheduleActivation(); // Draft → Pending
      contract.Activate();           // Pending → Active

      Assert.Equal(ContractStatus.Active, contract.Status);
    }

    [Fact]
    public void Activate_WhenAlreadyActive_ShouldThrowInvalidOperationException()
    {
      var contract = CreateDraftContract();
      contract.Activate();

      Assert.Throws<InvalidOperationException>(() => contract.Activate());
    }

    [Fact]
    public void Activate_WhenExpired_ShouldThrowInvalidOperationException()
    {
      var contract = CreateDraftContract();
      contract.Activate();
      contract.Expire(new DateTime(2026, 12, 31));

      Assert.Throws<InvalidOperationException>(() => contract.Activate());
    }

    [Fact]
    public void Activate_WhenTerminated_ShouldThrowInvalidOperationException()
    {
      var contract = CreateDraftContract();
      contract.Activate();
      contract.Terminate("Resigned", DateTime.UtcNow);

      Assert.Throws<InvalidOperationException>(() => contract.Activate());
    }

    // ─── Expire ─────────────────────────────────────────────────────────

    [Fact]
    public void Expire_WhenActive_ShouldTransitionToExpiredAndSetEndDate()
    {
      var contract = CreateDraftContract();
      contract.Activate();
      var endDate = new DateTime(2026, 12, 31);

      contract.Expire(endDate);

      Assert.Equal(ContractStatus.Expired, contract.Status);
      Assert.Equal(endDate, contract.EndDate);
    }

    [Fact]
    public void Expire_WhenDraft_ShouldThrowInvalidOperationException()
    {
      var contract = CreateDraftContract();
      Assert.Throws<InvalidOperationException>(() => contract.Expire(DateTime.UtcNow));
    }

    [Fact]
    public void Expire_WhenPending_ShouldThrowInvalidOperationException()
    {
      var contract = CreateDraftContract();
      contract.ScheduleActivation();

      Assert.Throws<InvalidOperationException>(() => contract.Expire(DateTime.UtcNow));
    }

    // ─── Terminate ──────────────────────────────────────────────────────

    [Fact]
    public void Terminate_WhenActive_ShouldTransitionToTerminated()
    {
      var contract = CreateDraftContract();
      contract.Activate();
      var terminatedAt = new DateTime(2026, 6, 15);

      contract.Terminate("Resignation", terminatedAt);

      Assert.Equal(ContractStatus.Terminated, contract.Status);
      Assert.Equal(terminatedAt, contract.EndDate);
    }

    [Fact]
    public void Terminate_WhenAlreadyTerminated_ShouldThrowInvalidOperationException()
    {
      var contract = CreateDraftContract();
      contract.Activate();
      contract.Terminate("First reason", DateTime.UtcNow);

      Assert.Throws<InvalidOperationException>(() => contract.Terminate("Second reason", DateTime.UtcNow));
    }

    [Fact]
    public void Terminate_WhenExpired_ShouldThrowInvalidOperationException()
    {
      var contract = CreateDraftContract();
      contract.Activate();
      contract.Expire(DateTime.UtcNow);

      Assert.Throws<InvalidOperationException>(() => contract.Terminate("Reason", DateTime.UtcNow));
    }

    // ─── Constructor validation ──────────────────────────────────────────

    [Theory]
    [InlineData("", "CTR-001")]
    [InlineData("emp1", "")]
    public void Constructor_WithMissingRequiredField_ShouldThrowArgumentException(string employeeId, string contractCode)
    {
      Assert.Throws<ArgumentException>(() =>
          new ContractEntity(employeeId, contractCode, DateTime.UtcNow));
    }

    [Fact]
    public void Constructor_ShouldDefaultToDraftStatus()
    {
      var contract = CreateDraftContract();
      Assert.Equal(ContractStatus.Draft, contract.Status);
    }
  }
}
