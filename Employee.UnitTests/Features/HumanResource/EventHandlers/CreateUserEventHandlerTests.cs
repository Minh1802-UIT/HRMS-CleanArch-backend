using Xunit;
using Moq;
using Employee.Application.Common.Interfaces;
using Employee.Application.Common.Models;
using Employee.Application.Features.HumanResource.EventHandlers;
using Employee.Domain.Events;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace Employee.UnitTests.Features.HumanResource.EventHandlers
{
  public class CreateUserEventHandlerTests
  {
    private readonly Mock<IBackgroundJobService> _mockJobService;
    private readonly Mock<ILogger<CreateUserEventHandler>> _mockLogger;
    private readonly CreateUserEventHandler _handler;

    public CreateUserEventHandlerTests()
    {
      _mockJobService = new Mock<IBackgroundJobService>();
      _mockLogger = new Mock<ILogger<CreateUserEventHandler>>();

      _handler = new CreateUserEventHandler(
          _mockJobService.Object,
          _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ShouldEnqueueAccountProvisioningJobWithCorrectParameters()
    {
      // Arrange
      var domainEvent = new EmployeeCreatedEvent("emp-001", "Alice Nguyen", "alice@hrm.com", "0901234567");
      var notification = new DomainEventNotification<EmployeeCreatedEvent>(domainEvent);

      // Act
      await _handler.Handle(notification, CancellationToken.None);

      // Assert — Hangfire job enqueued with the exact values from the domain event
      _mockJobService.Verify(
          x => x.EnqueueAccountProvisioning(
              "emp-001",
              "alice@hrm.com",
              "Alice Nguyen",
              "0901234567"),
          Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnCompletedTask_WithoutAwaitingJobExecution()
    {
      // The handler is intentionally synchronous — it enqueues a job and returns
      // immediately. No I/O happens inside the handle call itself.
      var domainEvent = new EmployeeCreatedEvent("emp-002", "Bob Tran", "bob@hrm.com", "0912345678");
      var notification = new DomainEventNotification<EmployeeCreatedEvent>(domainEvent);

      var task = _handler.Handle(notification, CancellationToken.None);

      Assert.True(task.IsCompleted);
      await task; // should not throw
    }

    [Fact]
    public async Task Handle_ShouldNotCallEnqueueMoreThanOnce_PerEvent()
    {
      // Ensure idempotent enqueue — exactly one job per EmployeeCreatedEvent
      var domainEvent = new EmployeeCreatedEvent("emp-003", "Carol Le", "carol@hrm.com", "0923456789");
      var notification = new DomainEventNotification<EmployeeCreatedEvent>(domainEvent);

      await _handler.Handle(notification, CancellationToken.None);

      _mockJobService.Verify(
          x => x.EnqueueAccountProvisioning(
              It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
          Times.Once);
    }
  }
}
