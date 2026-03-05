using Xunit;
using Moq;
using Employee.Application.Features.Attendance.Services;
using Employee.Domain.Interfaces.Repositories;
using Employee.Application.Common.Interfaces.Attendance.IService;
using Employee.Domain.Entities.HumanResource;
using Employee.Domain.Entities.Attendance;
using Employee.Application.Features.Attendance.Logic;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Threading;

namespace Employee.UnitTests.Features.Attendance
{
  public class AttendanceServiceTests
  {
    private readonly Mock<IAttendanceRepository> _mockAttendanceRepo;
    private readonly Mock<IEmployeeRepository> _mockEmployeeRepo;

    private readonly AttendanceService _service;

    public AttendanceServiceTests()
    {
      _mockAttendanceRepo = new Mock<IAttendanceRepository>();
      _mockEmployeeRepo = new Mock<IEmployeeRepository>();

      _service = new AttendanceService(
          _mockAttendanceRepo.Object,
          _mockEmployeeRepo.Object
      );
    }

    [Fact]
    public async Task GetTeamAttendanceSummaryAsync_ShouldUseManagerIdFilter_OPT3()
    {
      // Arrange
      var managerId = "mgr_001";
      var fromDate = new DateTime(2026, 2, 1);
      var toDate = new DateTime(2026, 2, 28);

      // Mock Employees (Direct Manager Filter)
      var emp1 = new EmployeeEntity("EMP001", "User 1", "u1@hrm.com");
      emp1.SetId("emp1");
      var emp2 = new EmployeeEntity("EMP002", "User 2", "u2@hrm.com");
      emp2.SetId("emp2");

      _mockEmployeeRepo.Setup(x => x.GetByManagerIdAsync(managerId, It.IsAny<CancellationToken>()))
          .ReturnsAsync(new List<EmployeeEntity> { emp1, emp2 });

      // Mock Attendance
      _mockAttendanceRepo.Setup(x => x.GetByMonthsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync(new List<AttendanceBucket>());

      // Act
      await _service.GetTeamAttendanceSummaryAsync(managerId, fromDate, toDate);

      // Assert
      // Verify GetByManagerIdAsync is called
      _mockEmployeeRepo.Verify(x => x.GetByManagerIdAsync(managerId, It.IsAny<CancellationToken>()), Times.Once);

      // Verify GetAllAsync is NOT called (No full table scan)
      _mockEmployeeRepo.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
  }
}
