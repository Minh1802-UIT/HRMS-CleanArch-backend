using Employee.Application.Common.Interfaces.Organization.IService;
using Employee.Domain.Entities.Notifications;

namespace Employee.Application.Features.Notifications.Mappers
{
  /// <summary>
  /// Extension methods for mapping Notification entity ↔ DTO.
  /// Follows the same convention as all other feature-level mappers.
  /// </summary>
  public static class NotificationMapper
  {
    public static NotificationDto ToDto(this Notification n) => new NotificationDto
    {
      Id = n.Id,
      UserId = n.UserId,
      Title = n.Title,
      Body = n.Body,
      Type = n.Type,
      IsRead = n.IsRead,
      ReferenceId = n.ReferenceId,
      ReferenceType = n.ReferenceType,
      CreatedAt = n.CreatedAt
    };
  }
}
