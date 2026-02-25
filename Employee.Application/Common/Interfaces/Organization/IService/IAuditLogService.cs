using Employee.Domain.Entities.Common;

namespace Employee.Application.Common.Interfaces.Organization.IService;

public interface IAuditLogService
{
    Task LogAsync(string userId, string userName, string action, string tableName, string recordId, object? oldVal, object? newVal);
    Task<List<AuditLog>> GetLogsAsync();
}