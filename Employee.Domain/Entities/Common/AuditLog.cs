namespace Employee.Domain.Entities.Common
{
    public class AuditLog : BaseEntity
    {
    public string UserId { get; private set; } = string.Empty;
    public string UserName { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty; // e.g., Create, Update, Delete, Login
    public string TableName { get; private set; } = string.Empty; // e.g., Employees, Contracts
    public string RecordId { get; private set; } = string.Empty;
    public string? OldValues { get; private set; } // Stored as JSON string
    public string? NewValues { get; private set; } // Stored as JSON string

    private AuditLog() { }

    public AuditLog(string userId, string userName, string action, string tableName, string recordId, string? oldValues = null, string? newValues = null)
    {
      UserId = userId;
      UserName = userName;
      Action = action;
      TableName = tableName;
      RecordId = recordId;
      OldValues = oldValues;
      NewValues = newValues;
    }
    }
}