using Employee.Domain.Entities.Common;
using System;

namespace Employee.Domain.Entities.Notifications
{
  /// <summary>
  /// In-app notification for a specific user.
  /// Created by domain event handlers (leave approved/rejected, payroll generated, etc.)
  /// </summary>
  public class Notification : BaseEntity
  {
    /// <summary>Target user's Identity UserId (from ASP.NET Identity, not employeeId).</summary>
    public string UserId { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;

    /// <summary>Logical type tag, e.g., "LeaveApproved", "LeaveRejected", "PayrollGenerated".</summary>
    public string Type { get; private set; } = string.Empty;

    public bool IsRead { get; private set; } = false;

    /// <summary>ID of the related entity (e.g., LeaveRequest.Id).</summary>
    public string? ReferenceId { get; private set; }

    /// <summary>Table/collection name of the related entity, e.g., "LeaveRequest".</summary>
    public string? ReferenceType { get; private set; }

    private Notification() { }

    public Notification(
        string userId,
        string title,
        string body,
        string type,
        string? referenceId = null,
        string? referenceType = null)
    {
      if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("UserId is required.");
      if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title is required.");
      if (string.IsNullOrWhiteSpace(body)) throw new ArgumentException("Body is required.");

      UserId = userId;
      Title = title;
      Body = body;
      Type = type;
      ReferenceId = referenceId;
      ReferenceType = referenceType;
      CreatedAt = DateTime.UtcNow;
    }

    public void MarkRead(DateTime readAt)
    {
      if (!IsRead)
      {
        IsRead = true;
        SetUpdatedAt(readAt);
      }
    }
  }
}
