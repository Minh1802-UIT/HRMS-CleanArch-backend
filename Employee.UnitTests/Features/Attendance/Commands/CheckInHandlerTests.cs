using Employee.Application.Common.Exceptions;
using Employee.Application.Common.Interfaces.Organization.IService;
using Employee.Application.Features.Attendance.Commands.CheckIn;
using Employee.Application.Features.Attendance.Dtos;
using Employee.Application.Features.Attendance.Services;
using Employee.Domain.Entities.Attendance;
using Employee.Domain.Enums;
using Employee.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Employee.UnitTests.Features.Attendance.Commands;

public class CheckInHandlerTests
{
  private readonly Mock<IRawAttendanceLogRepository> _rawRepo = new();
  private readonly Mock<IAttendanceProcessingService> _processing = new();
  private readonly Mock<IHostEnvironment> _env = new();
  private readonly CheckInHandler _handler;

  public CheckInHandlerTests()
  {
    // Create a real CheckInVerificationService with mocked dependencies
    var verificationService = new CheckInVerificationService(
        Mock.Of<IOfficeLocationRepository>(),
        Mock.Of<IWfhApprovalRepository>(),
        _rawRepo.Object,
        Mock.Of<ILogger<CheckInVerificationService>>());

    _handler = new CheckInHandler(
        _rawRepo.Object,
        _processing.Object,
        verificationService,
        Mock.Of<ILogger<CheckInHandler>>(),
        _env.Object);
  }

  private static CheckInCommand Command(string employeeId = "emp-1") => new()
  {
    EmployeeId = employeeId,
    Dto = new CheckInRequestDto { Type = "CheckIn", DeviceId = "unit-test" }
  };

  [Fact]
  public async Task Handle_WhenRecentLogExists_NonTestingEnvironment_ShouldThrowConflictException()
  {
    _env.Setup(e => e.EnvironmentName).Returns("Development");
    var recent = new RawAttendanceLog(
        "emp-1",
        DateTime.UtcNow.AddSeconds(-30),
        RawLogType.CheckIn,
        "dev");
    _rawRepo
        .Setup(r => r.GetLatestLogAsync("emp-1", It.IsAny<CancellationToken>()))
        .ReturnsAsync(recent);

    await Assert.ThrowsAsync<ConflictException>(() =>
        _handler.Handle(Command(), CancellationToken.None));

    _rawRepo.Verify(
        r => r.CreateAsync(It.IsAny<RawAttendanceLog>(), It.IsAny<CancellationToken>()),
        Times.Never);
  }

  [Fact]
  public async Task Handle_WhenRecentLogExists_TestingEnvironment_ShouldAllowPunch()
  {
    _processing.Setup(p => p.ProcessRawLogsAsync()).ReturnsAsync("ok");
    _env.Setup(e => e.EnvironmentName).Returns("Testing");
    _rawRepo
        .Setup(r => r.GetLatestLogAsync("emp-1", It.IsAny<CancellationToken>()))
        .ReturnsAsync(new RawAttendanceLog(
            "emp-1",
            DateTime.UtcNow.AddSeconds(-10),
            RawLogType.CheckIn,
            "dev"));

    await _handler.Handle(Command(), CancellationToken.None);

    _rawRepo.Verify(
        r => r.GetLatestLogAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
        Times.Never);
    _rawRepo.Verify(
        r => r.CreateAsync(It.IsAny<RawAttendanceLog>(), It.IsAny<CancellationToken>()),
        Times.Once);
    _processing.Verify(p => p.ProcessRawLogsAsync(), Times.Once);
  }
}
