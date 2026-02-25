using Xunit;
using Moq;
using Employee.Application.Features.Leave.Services;
using Employee.Application.Common.Interfaces.Organization.IRepository;
using Employee.Application.Common.Interfaces.Organization.IService;
using Employee.Domain.Entities.Leave;
using Employee.Domain.Entities.HumanResource;
using Employee.Domain.Entities.ValueObjects;
using Employee.Application.Common.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;
using Employee.Domain.Enums;
using System.Threading;

namespace Employee.UnitTests.Features.Leave
{
  public class LeaveAllocationServiceTests
  {
    private readonly Mock<ILeaveAllocationRepository> _mockAllocationRepo;
    private readonly Mock<ILeaveTypeRepository> _mockLeaveTypeRepo;
    private readonly Mock<IEmployeeRepository> _mockEmployeeRepo;

    private readonly LeaveAllocationService _service;

    public LeaveAllocationServiceTests()
    {
      _mockAllocationRepo = new Mock<ILeaveAllocationRepository>();
      _mockLeaveTypeRepo = new Mock<ILeaveTypeRepository>();
      _mockEmployeeRepo = new Mock<IEmployeeRepository>();

      _service = new LeaveAllocationService(
          _mockAllocationRepo.Object,
          _mockLeaveTypeRepo.Object,
          _mockEmployeeRepo.Object
      );
    }

    [Fact]
    public async Task RunMonthlyAccrualAsync_ShouldProcessActiveAndProbationEmployees()
    {
      // Arrange — 2 eligible employees (Active + Probation)
      var emp1 = new EmployeeEntity("EMP001", "User 1", "u1@hrm.com");
      emp1.SetId("emp1");
      emp1.UpdateJobDetails(new JobDetails { Status = EmployeeStatus.Active });

      var emp2 = new EmployeeEntity("EMP002", "User 2", "u2@hrm.com");
      emp2.SetId("emp2");
      emp2.UpdateJobDetails(new JobDetails { Status = EmployeeStatus.Probation });

      _mockEmployeeRepo.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
          .ReturnsAsync(new List<EmployeeEntity> { emp1, emp2 });

      var accrualType = new LeaveType("Annual Leave", "AL", 12);
      accrualType.SetId("AL");
      accrualType.UpdateSettings(true, 1.0, false, 0);

      _mockLeaveTypeRepo.Setup(x => x.GetPagedAsync(It.IsAny<PaginationParams>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync(new PagedResult<LeaveType> { Items = new List<LeaveType> { accrualType } });

      // MISS-5 FIX: mock the bulk-fetch method actually called by the service
      _mockAllocationRepo
          .Setup(x => x.GetByEmployeeIdsAndYearAsync(It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync(new List<LeaveAllocation>());

      _mockAllocationRepo
          .Setup(x => x.BulkUpsertAsync(It.IsAny<List<LeaveAllocation>>(), It.IsAny<CancellationToken>()))
          .Returns(Task.CompletedTask);

      // Act
      await _service.RunMonthlyAccrualAsync();

      // Assert
      _mockEmployeeRepo.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
      _mockEmployeeRepo.Verify(x => x.GetPagedAsync(It.IsAny<PaginationParams>(), It.IsAny<CancellationToken>()), Times.Never);

      // Both Active and Probation employees receive one allocation each
      _mockAllocationRepo.Verify(
          x => x.BulkUpsertAsync(
              It.Is<List<LeaveAllocation>>(list => list.Count == 2),
              It.IsAny<CancellationToken>()),
          Times.Once);
    }

    [Fact]
    public async Task RunMonthlyAccrualAsync_ShouldSkipResignedAndTerminatedEmployees()
    {
      // MISS-5 FIX: verify the status filter excludes Resigned / Terminated employees
      var activeEmp = new EmployeeEntity("EMP001", "Active", "a@hrm.com");
      activeEmp.SetId("emp-active");
      activeEmp.UpdateJobDetails(new JobDetails { Status = EmployeeStatus.Active });

      var resignedEmp = new EmployeeEntity("EMP002", "Resigned", "r@hrm.com");
      resignedEmp.SetId("emp-resigned");
      resignedEmp.UpdateJobDetails(new JobDetails { Status = EmployeeStatus.Resigned });

      var terminatedEmp = new EmployeeEntity("EMP003", "Terminated", "t@hrm.com");
      terminatedEmp.SetId("emp-terminated");
      terminatedEmp.UpdateJobDetails(new JobDetails { Status = EmployeeStatus.Terminated });

      _mockEmployeeRepo.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
          .ReturnsAsync(new List<EmployeeEntity> { activeEmp, resignedEmp, terminatedEmp });

      var accrualType = new LeaveType("Annual Leave", "AL", 12);
      accrualType.SetId("AL");
      accrualType.UpdateSettings(true, 1.0, false, 0);

      _mockLeaveTypeRepo.Setup(x => x.GetPagedAsync(It.IsAny<PaginationParams>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync(new PagedResult<LeaveType> { Items = new List<LeaveType> { accrualType } });

      _mockAllocationRepo
          .Setup(x => x.GetByEmployeeIdsAndYearAsync(It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync(new List<LeaveAllocation>());

      _mockAllocationRepo
          .Setup(x => x.BulkUpsertAsync(It.IsAny<List<LeaveAllocation>>(), It.IsAny<CancellationToken>()))
          .Returns(Task.CompletedTask);

      // Act
      await _service.RunMonthlyAccrualAsync();

      // Assert: only 1 allocation (the active employee) should be upserted
      _mockAllocationRepo.Verify(
          x => x.BulkUpsertAsync(
              It.Is<List<LeaveAllocation>>(list =>
                  list.Count == 1 &&
                  list[0].EmployeeId == "emp-active"),
              It.IsAny<CancellationToken>()),
          Times.Once);
    }

    [Fact]
    public async Task RunMonthlyAccrualAsync_ShouldSkipEmployeeAlreadyAccruedThisMonth()
    {
      // MISS-5 FIX: idempotency — re-running in the same month must not double-accrue
      var emp = new EmployeeEntity("EMP001", "User", "user@hrm.com");
      emp.SetId("emp1");
      emp.UpdateJobDetails(new JobDetails { Status = EmployeeStatus.Active });

      _mockEmployeeRepo.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
          .ReturnsAsync(new List<EmployeeEntity> { emp });

      var accrualType = new LeaveType("Annual Leave", "AL", 12);
      accrualType.SetId("AL");
      accrualType.UpdateSettings(true, 1.0, false, 0);

      _mockLeaveTypeRepo.Setup(x => x.GetPagedAsync(It.IsAny<PaginationParams>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync(new PagedResult<LeaveType> { Items = new List<LeaveType> { accrualType } });

      // Existing allocation already has the current month as LastAccrualMonth
      var existingAllocation = new LeaveAllocation("emp1", "AL", DateTime.UtcNow.Year.ToString(), 0);
      existingAllocation.UpdateAccrual(1.0, DateTime.UtcNow.ToString("yyyy-MM"));

      _mockAllocationRepo
          .Setup(x => x.GetByEmployeeIdsAndYearAsync(It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync(new List<LeaveAllocation> { existingAllocation });

      _mockAllocationRepo
          .Setup(x => x.BulkUpsertAsync(It.IsAny<List<LeaveAllocation>>(), It.IsAny<CancellationToken>()))
          .Returns(Task.CompletedTask);

      // Act
      await _service.RunMonthlyAccrualAsync();

      // Assert: BulkUpsert called with empty list — nothing to update
      _mockAllocationRepo.Verify(
          x => x.BulkUpsertAsync(
              It.Is<List<LeaveAllocation>>(list => list.Count == 0),
              It.IsAny<CancellationToken>()),
          Times.Once);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // NEW-5: RunYearEndCarryForwardAsync tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task RunYearEndCarryForwardAsync_ShouldCarryForwardUpToMaxDays()
    {
      // Arrange — employee has 5 unused days but MaxCarryForwardDays=3 → carry 3
      var emp = new EmployeeEntity("EMP001", "Alice", "alice@hrm.com");
      emp.SetId("emp1");
      emp.UpdateJobDetails(new JobDetails { Status = EmployeeStatus.Active });

      _mockEmployeeRepo.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
          .ReturnsAsync(new List<EmployeeEntity> { emp });

      var carryType = new LeaveType("Annual Leave", "AL", 12);
      carryType.SetId("AL");
      carryType.UpdateSettings(false, 0, allowCarryForward: true, maxCarry: 3);

      _mockLeaveTypeRepo.Setup(x => x.GetPagedAsync(It.IsAny<PaginationParams>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync(new PagedResult<LeaveType> { Items = new List<LeaveType> { carryType } });

      // fromYear allocation: granted=10, used=5 → CurrentBalance=5
      var fromAlloc = new LeaveAllocation("emp1", "AL", "2025", 10);
      fromAlloc.RecordUsage(5);

      _mockAllocationRepo
          .Setup(x => x.GetByEmployeeIdsAndYearAsync(It.IsAny<List<string>>(), "2025", It.IsAny<CancellationToken>()))
          .ReturnsAsync(new List<LeaveAllocation> { fromAlloc });

      _mockAllocationRepo
          .Setup(x => x.GetByEmployeeIdsAndYearAsync(It.IsAny<List<string>>(), "2026", It.IsAny<CancellationToken>()))
          .ReturnsAsync(new List<LeaveAllocation>());

      List<LeaveAllocation>? upserted = null;
      _mockAllocationRepo
          .Setup(x => x.BulkUpsertAsync(It.IsAny<List<LeaveAllocation>>(), It.IsAny<CancellationToken>()))
          .Callback<List<LeaveAllocation>, CancellationToken>((list, _) => upserted = list)
          .Returns(Task.CompletedTask);

      // Act
      var count = await _service.RunYearEndCarryForwardAsync(2025);

      // Assert — exactly 1 record upserted, carrying max 3 days (not 5)
      Assert.Equal(1, count);
      Assert.NotNull(upserted);
      Assert.Single(upserted!);
      Assert.Equal("emp1", upserted![0].EmployeeId);
      Assert.Equal("AL", upserted![0].LeaveTypeId);
      Assert.Equal("2026", upserted![0].Year);
      Assert.Equal(3, upserted![0].NumberOfDays);
    }

    [Fact]
    public async Task RunYearEndCarryForwardAsync_ShouldSkipEmployeeWithZeroBalance()
    {
      // Arrange — employee fully consumed all leave days
      var emp = new EmployeeEntity("EMP001", "Bob", "bob@hrm.com");
      emp.SetId("emp1");
      emp.UpdateJobDetails(new JobDetails { Status = EmployeeStatus.Active });

      _mockEmployeeRepo.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
          .ReturnsAsync(new List<EmployeeEntity> { emp });

      var carryType = new LeaveType("Annual Leave", "AL", 12);
      carryType.SetId("AL");
      carryType.UpdateSettings(false, 0, allowCarryForward: true, maxCarry: 5);

      _mockLeaveTypeRepo.Setup(x => x.GetPagedAsync(It.IsAny<PaginationParams>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync(new PagedResult<LeaveType> { Items = new List<LeaveType> { carryType } });

      // fromYear allocation: granted=10, used=10 → CurrentBalance=0
      var fromAlloc = new LeaveAllocation("emp1", "AL", "2025", 10);
      fromAlloc.RecordUsage(10);

      _mockAllocationRepo
          .Setup(x => x.GetByEmployeeIdsAndYearAsync(It.IsAny<List<string>>(), "2025", It.IsAny<CancellationToken>()))
          .ReturnsAsync(new List<LeaveAllocation> { fromAlloc });

      _mockAllocationRepo
          .Setup(x => x.GetByEmployeeIdsAndYearAsync(It.IsAny<List<string>>(), "2026", It.IsAny<CancellationToken>()))
          .ReturnsAsync(new List<LeaveAllocation>());

      // Act
      var count = await _service.RunYearEndCarryForwardAsync(2025);

      // Assert — nothing to carry forward
      Assert.Equal(0, count);
      _mockAllocationRepo.Verify(
          x => x.BulkUpsertAsync(It.IsAny<List<LeaveAllocation>>(), It.IsAny<CancellationToken>()),
          Times.Never);
    }

    [Fact]
    public async Task RunYearEndCarryForwardAsync_ShouldNotProcessLeaveTypeWithCarryForwardDisabled()
    {
      // Arrange — leave type has AllowCarryForward=false
      var emp = new EmployeeEntity("EMP001", "Carol", "carol@hrm.com");
      emp.SetId("emp1");
      emp.UpdateJobDetails(new JobDetails { Status = EmployeeStatus.Active });

      _mockEmployeeRepo.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
          .ReturnsAsync(new List<EmployeeEntity> { emp });

      var nonCarryType = new LeaveType("Sick Leave", "SL", 12);
      nonCarryType.SetId("SL");
      nonCarryType.UpdateSettings(false, 0, allowCarryForward: false, maxCarry: 0);

      _mockLeaveTypeRepo.Setup(x => x.GetPagedAsync(It.IsAny<PaginationParams>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync(new PagedResult<LeaveType> { Items = new List<LeaveType> { nonCarryType } });

      // Act — no allocation repo calls should happen since no qualifying leave type
      var count = await _service.RunYearEndCarryForwardAsync(2025);

      // Assert — returns 0, no repo queries issued
      Assert.Equal(0, count);
      _mockAllocationRepo.Verify(
          x => x.GetByEmployeeIdsAndYearAsync(It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
          Times.Never);
    }

    [Fact]
    public async Task RunYearEndCarryForwardAsync_ShouldAddCarryDaysToExistingNextYearAllocation()
    {
      // Arrange — employee already has a next-year allocation; carry-forward adds on top
      var emp = new EmployeeEntity("EMP001", "Dave", "dave@hrm.com");
      emp.SetId("emp1");
      emp.UpdateJobDetails(new JobDetails { Status = EmployeeStatus.Active });

      _mockEmployeeRepo.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
          .ReturnsAsync(new List<EmployeeEntity> { emp });

      var carryType = new LeaveType("Annual Leave", "AL", 12);
      carryType.SetId("AL");
      carryType.UpdateSettings(false, 0, allowCarryForward: true, maxCarry: 4);

      _mockLeaveTypeRepo.Setup(x => x.GetPagedAsync(It.IsAny<PaginationParams>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync(new PagedResult<LeaveType> { Items = new List<LeaveType> { carryType } });

      // fromYear: 6 unused days (will carry min(6,4)=4)
      var fromAlloc = new LeaveAllocation("emp1", "AL", "2025", 10);
      fromAlloc.RecordUsage(4);

      // toYear: already has 12 days granted
      var toAlloc = new LeaveAllocation("emp1", "AL", "2026", 12);

      _mockAllocationRepo
          .Setup(x => x.GetByEmployeeIdsAndYearAsync(It.IsAny<List<string>>(), "2025", It.IsAny<CancellationToken>()))
          .ReturnsAsync(new List<LeaveAllocation> { fromAlloc });

      _mockAllocationRepo
          .Setup(x => x.GetByEmployeeIdsAndYearAsync(It.IsAny<List<string>>(), "2026", It.IsAny<CancellationToken>()))
          .ReturnsAsync(new List<LeaveAllocation> { toAlloc });

      List<LeaveAllocation>? upserted = null;
      _mockAllocationRepo
          .Setup(x => x.BulkUpsertAsync(It.IsAny<List<LeaveAllocation>>(), It.IsAny<CancellationToken>()))
          .Callback<List<LeaveAllocation>, CancellationToken>((list, _) => upserted = list)
          .Returns(Task.CompletedTask);

      // Act
      var count = await _service.RunYearEndCarryForwardAsync(2025);

      // Assert — existing 2026 allocation updated to 12 + 4 = 16
      Assert.Equal(1, count);
      Assert.NotNull(upserted);
      Assert.Equal(16, upserted![0].NumberOfDays);
    }

    [Fact]
    public async Task RunYearEndCarryForwardAsync_ShouldReturnCorrectCountForMultipleEmployees()
    {
      // Arrange — 3 employees: 2 with remaining balance, 1 with zero balance
      var emps = new List<EmployeeEntity>();
      for (int i = 1; i <= 3; i++)
      {
        var e = new EmployeeEntity($"EMP00{i}", $"User{i}", $"u{i}@hrm.com");
        e.SetId($"emp{i}");
        e.UpdateJobDetails(new JobDetails { Status = EmployeeStatus.Active });
        emps.Add(e);
      }

      _mockEmployeeRepo.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
          .ReturnsAsync(emps);

      var carryType = new LeaveType("Annual Leave", "AL", 12);
      carryType.SetId("AL");
      carryType.UpdateSettings(false, 0, allowCarryForward: true, maxCarry: 5);

      _mockLeaveTypeRepo.Setup(x => x.GetPagedAsync(It.IsAny<PaginationParams>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync(new PagedResult<LeaveType> { Items = new List<LeaveType> { carryType } });

      // emp1: 3 days remaining, emp2: 0 days (fully used), emp3: 2 days remaining
      var alloc1 = new LeaveAllocation("emp1", "AL", "2025", 10); alloc1.RecordUsage(7);  // 3 left
      var alloc2 = new LeaveAllocation("emp2", "AL", "2025", 10); alloc2.RecordUsage(10); // 0 left
      var alloc3 = new LeaveAllocation("emp3", "AL", "2025", 10); alloc3.RecordUsage(8);  // 2 left

      _mockAllocationRepo
          .Setup(x => x.GetByEmployeeIdsAndYearAsync(It.IsAny<List<string>>(), "2025", It.IsAny<CancellationToken>()))
          .ReturnsAsync(new List<LeaveAllocation> { alloc1, alloc2, alloc3 });

      _mockAllocationRepo
          .Setup(x => x.GetByEmployeeIdsAndYearAsync(It.IsAny<List<string>>(), "2026", It.IsAny<CancellationToken>()))
          .ReturnsAsync(new List<LeaveAllocation>());

      _mockAllocationRepo
          .Setup(x => x.BulkUpsertAsync(It.IsAny<List<LeaveAllocation>>(), It.IsAny<CancellationToken>()))
          .Returns(Task.CompletedTask);

      // Act
      var count = await _service.RunYearEndCarryForwardAsync(2025);

      // Assert — only emp1 and emp3 qualify (emp2 has 0 balance)
      Assert.Equal(2, count);
    }
  }
}
