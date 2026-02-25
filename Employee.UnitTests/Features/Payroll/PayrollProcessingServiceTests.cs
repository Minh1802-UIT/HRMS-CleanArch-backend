using Xunit;
using Moq;
using Employee.Application.Features.Payroll.Services;
using Employee.Application.Common.Interfaces;
using Employee.Application.Common.Interfaces.Organization.IRepository;
using Employee.Application.Common.Interfaces.Organization.IService;
using Employee.Domain.Entities.Payroll;
using Employee.Domain.Entities.HumanResource;
using Employee.Domain.Entities.Attendance;
using Employee.Domain.Entities.ValueObjects;
using Employee.Application.Common.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;
using Employee.Domain.Enums;
using Employee.Domain.Services.Payroll;

namespace Employee.UnitTests.Features.Payroll
{
  public class PayrollProcessingServiceTests
  {
    private readonly Mock<IPayrollRepository> _mockPayrollRepo;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IPayrollDataProvider> _mockDataProvider;
    private readonly Mock<ITaxCalculator> _mockTaxCalculator;

    private readonly PayrollProcessingService _service;

    public PayrollProcessingServiceTests()
    {
      _mockPayrollRepo = new Mock<IPayrollRepository>();
      _mockUnitOfWork = new Mock<IUnitOfWork>();
      _mockDataProvider = new Mock<IPayrollDataProvider>();
      _mockTaxCalculator = new Mock<ITaxCalculator>();

      _service = new PayrollProcessingService(
          _mockPayrollRepo.Object,
          _mockUnitOfWork.Object,
          _mockDataProvider.Object,
          _mockTaxCalculator.Object
      );
    }

    [Fact]
    public async Task CalculatePayrollAsync_ShouldCallDataProviderFetch()
    {
      // Arrange
      var month = "02";
      var year = "2026";

      _mockDataProvider.Setup(x => x.FetchCalculationDataAsync(month, year))
          .ReturnsAsync(new PayrollDataContainer { MonthKey = "02-2026" });

      // Act
      await _service.CalculatePayrollAsync(month, year);

      // Assert
      _mockDataProvider.Verify(x => x.FetchCalculationDataAsync(month, year), Times.Once);
    }
  }
}
