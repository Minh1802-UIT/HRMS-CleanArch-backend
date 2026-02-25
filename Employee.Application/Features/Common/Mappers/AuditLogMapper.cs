using Employee.Application.Features.Common.Dtos;
using Employee.Domain.Entities.Common;

namespace Employee.Application.Features.Common.Mappers
{
    public static class AuditLogMapper
    {
        public static AuditLogDto ToDto(this AuditLog log)
        {
            return new AuditLogDto
            {
                Id = log.Id,
                UserId = log.UserId,
                UserName = log.UserName,
                Action = log.Action,
                TableName = log.TableName,
                RecordId = log.RecordId,
                OldValues = log.OldValues,
                NewValues = log.NewValues,
                CreatedAt = log.CreatedAt
            };
        }
    }
}
