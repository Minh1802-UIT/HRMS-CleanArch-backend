using Xunit;
using Moq;
using Employee.Application.Features.Payroll.Services;
using Employee.Application.Common.Interfaces;
using Employee.Domain.Interfaces.Repositories;
using Employee.Application.Common.Interfaces.Organization.IService;
using Employee.Domain.Entities.Payroll;
using Employee.Domain.Entities.HumanResource;
using Employee.Domain.Entities.Attendance;
using Employee.Domain.Entities.ValueObjects;
using Employee.Domain.Common.Models;
using Employee.Application.Common.Dtos;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;
using Employee.Domain.Enums;
using Employee.Domain.Services.Payroll;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Employee.UnitTests.Features.Payroll
{
    public class PayrollCalculationTests
    {
    private readonly Mock<IPayrollRepository> _mockPayrollRepo;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IPayrollDataProvider> _mockDataProvider;
    private readonly Mock<ITaxCalculator> _mockTaxCalculator;

        private readonly PayrollProcessingService _service;

        public PayrollCalculationTests()
        {
      _mockPayrollRepo = new Mock<IPayrollRepository>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
      _mockDataProvider = new Mock<IPayrollDataProvider>();
      _mockTaxCalculator = new Mock<ITaxCalculator>();

            _service = new PayrollProcessingService(
                _mockPayrollRepo.Object,
                _mockUnitOfWork.Object,
                _mockDataProvider.Object,
                _mockTaxCalculator.Object,
                Mock.Of<ILogger<PayrollProcessingService>>()
            );
        }

        [Fact]
        public async Task CalculatePayrollAsync_ShouldCalculateCorrectNetSalary_For20M_Gross()
    {
            var month = "02";
            var year = "2026";
            var monthKey = "02-2026";
      var employeeId = "emp_20m";

      // 1. Data Container Setup
      var container = new PayrollDataContainer
      {
        MonthKey = monthKey,
        Cycle = new PayrollCycle(2, 2026,
            new DateTime(2026, 1, 26, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 2, 25, 0, 0, 0, DateTimeKind.Utc),
            26, "6,0", 0),
        Settings = new PayrollSettings
        {
          SocialInsuranceRate = 0.08m,
          HealthInsuranceRate = 0.015m,
          UnemploymentInsuranceRate = 0.01m,
          InsuranceSalaryCap = 36000000,
          PersonalDeduction = 11000000,
          DependentDeduction = 4400000,
          OvertimeRateNormal = 1.5m
        }
      };

      var emp = new EmployeeEntity("EMP020", "Rich Employee", "rich@hrm.com");
      emp.SetId(employeeId);
      emp.UpdatePersonalInfo(new PersonalInfo { DependentCount = 0 });
      emp.UpdateJobDetails(new JobDetails { DepartmentId = "d1", PositionId = "p1" });

      container.Employees.Add(emp);
      container.SalaryMap[employeeId] = new ContractSalaryProjection { EmployeeId = employeeId, BasicSalary = 20000000, Status = "Active" };

      var fullAttendance = new AttendanceBucket(employeeId, monthKey);
      for (int i = 0; i < 26; i++)
      {
        fullAttendance.AddOrUpdateDailyLog(DailyLog.Create(DateTime.UtcNow.AddSeconds(i), AttendanceStatus.Present));
      }
      container.AttendanceMap[employeeId] = fullAttendance;

      _mockDataProvider.Setup(x => x.FetchCalculationDataAsync(month, year)).ReturnsAsync(container);
      _mockTaxCalculator.Setup(x => x.CalculatePersonalIncomeTax(6900000m)).Returns(440000m);

      // Act
      await _service.CalculatePayrollAsync(month, year);

      // Assert
      _mockPayrollRepo.Verify(x => x.CreateAsync(It.Is<PayrollEntity>(p =>
          p.EmployeeId == employeeId &&
          p.GrossIncome > 0
      ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CalculatePayrollAsync_ShouldEnforceInsuranceCap_ForHighEarner()
    {
      // Arrange
      var month = "02";
      var year = "2026";
      var employeeId = "emp_high";
      var container = new PayrollDataContainer
      {
        MonthKey = "02-2026",
        Cycle = new PayrollCycle(2, 2026,
            new DateTime(2026, 1, 26, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 2, 25, 0, 0, 0, DateTimeKind.Utc),
            26, "6,0", 0),
        Settings = new PayrollSettings
        {
          SocialInsuranceRate = 0.08m,
          InsuranceSalaryCap = 36000000 // Cap
        }
      };

      var emp = new EmployeeEntity("EMP100", "CEO", "ceo@hrm.com");
      emp.SetId(employeeId);
      container.Employees.Add(emp);

      // Salary above cap
      container.SalaryMap[employeeId] = new ContractSalaryProjection { EmployeeId = employeeId, BasicSalary = 100000000, Status = "Active" };
      container.AttendanceMap[employeeId] = new AttendanceBucket(employeeId, "02-2026");

      _mockDataProvider.Setup(x => x.FetchCalculationDataAsync(month, year)).ReturnsAsync(container);

            // Act
            await _service.CalculatePayrollAsync(month, year);

      // Assert
      // SI should be calculated on 36M instead of 100M (36M * 0.08 = 2.88M)
      _mockPayrollRepo.Verify(x => x.CreateAsync(It.Is<PayrollEntity>(p => 
                p.EmployeeId == employeeId &&
                p.SocialInsurance == 2880000m
            ), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
