using Employee.Application.Common.Interfaces.Organization.IRepository;
using Employee.Application.Common.Interfaces.Organization.IService;
using Employee.Domain.Entities.Common;
using System.Text.Json;

namespace Employee.Application.Common.Services
{
  public class AuditLogService : IAuditLogService
  {
    private readonly IAuditLogRepository _repo;

    public AuditLogService(IAuditLogRepository repo)
    {
      _repo = repo;
    }

    public async Task LogAsync(string userId, string userName, string action, string tableName, string recordId, object? oldVal, object? newVal)
    {
      var log = new AuditLog(
        userId,
        userName,
        action,
        tableName,
        recordId,
        oldVal != null ? JsonSerializer.Serialize(oldVal) : null,
        newVal != null ? JsonSerializer.Serialize(newVal) : null
      );

      await _repo.CreateAsync(log);
    }

    public async Task<List<AuditLog>> GetLogsAsync() => await _repo.GetLogsAsync(100);
  }
}