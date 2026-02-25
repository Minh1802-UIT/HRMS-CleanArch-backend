using MediatR;
using Employee.Domain.Enums;

namespace Employee.Application.Features.Leave.Events
{
  /// <summary>
  /// Raised when an employee submits a new leave request.
  /// Handlers can react to: audit logging, manager notifications, etc.
  /// </summary>
  public record LeaveRequestSubmittedEvent(
      string LeaveRequestId,
      string EmployeeId,
      LeaveTypeEnum LeaveType,
      DateTime FromDate,
      DateTime ToDate,
      double Days,
      string Reason
  ) : INotification;
}
