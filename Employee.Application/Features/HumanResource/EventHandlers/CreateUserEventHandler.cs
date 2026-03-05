using Employee.Application.Common.Interfaces;
using Employee.Application.Common.Models;
using Employee.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Employee.Application.Features.HumanResource.EventHandlers
{
    public class CreateUserEventHandler : INotificationHandler<DomainEventNotification<EmployeeCreatedEvent>>
    {
    private readonly IBackgroundJobService _backgroundJobService;
    private readonly ILogger<CreateUserEventHandler> _logger;

    public CreateUserEventHandler(
        IBackgroundJobService backgroundJobService,
        ILogger<CreateUserEventHandler> logger)
    {
      _backgroundJobService = backgroundJobService;
      _logger = logger;
        }

        public Task Handle(DomainEventNotification<EmployeeCreatedEvent> notificationWrapper, CancellationToken cancellationToken)
        {
            var evt = notificationWrapper.DomainEvent;

      // Enqueue a persistent, retryable job instead of fire-and-forget Task.Run.
      // Hangfire persists the job in MongoDB — if the Identity server is down,
      // the worker retries automatically (up to 5 times with exponential back-off).
      _backgroundJobService.EnqueueAccountProvisioning(evt.EmployeeId, evt.Email, evt.FullName, evt.Phone);

      _logger.LogInformation(
          "Account provisioning job enqueued for employee {EmployeeId} ({Email}).",
          evt.EmployeeId, evt.Email);

      return Task.CompletedTask;
        }
    }
}


