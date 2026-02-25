using MediatR;

namespace Employee.Application.Features.Leave.Events
{
  /// <summary>
  /// Raised when a manager rejects a leave request.
  /// Handlers can react to: notifying the employee, audit trail, etc.
  /// </summary>
  public record LeaveRequestRejectedEvent(
      string LeaveRequestId,
      string EmployeeId,
      string RejectedBy,
      string RejectionReason
  ) : INotification;
}
