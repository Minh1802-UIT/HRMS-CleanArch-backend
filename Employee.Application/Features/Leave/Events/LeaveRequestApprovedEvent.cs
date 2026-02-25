using MediatR;

namespace Employee.Application.Features.Leave.Events
{
  /// <summary>
  /// Raised when a manager approves a leave request.
  /// Handlers can react to: notifying the employee, updating stats, etc.
  /// </summary>
  public record LeaveRequestApprovedEvent(
      string LeaveRequestId,
      string EmployeeId,
      string ApprovedBy,
      string? ManagerComment,
      double WorkingDaysDeducted
  ) : INotification;
}
