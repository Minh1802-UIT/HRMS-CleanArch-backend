using Employee.Domain.Entities.Common;
using System;

namespace Employee.Domain.Entities.Organization
{
  public class Department : BaseEntity
  {
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    // References
    public string? ManagerId { get; private set; }
    public string? ParentId { get; private set; } // Recursive relationship

    private Department() { }

    public Department(string name, string code)
    {
      if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.");
      if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Code is required.");

      Name = name;
      Code = code;
      CreatedAt = DateTime.UtcNow;
    }

    public void UpdateInfo(string name, string description)
    {
      Name = name;
      Description = description;
    }

    public void AssignManager(string? managerId)
    {
      ManagerId = managerId;
    }

    public void SetParent(string? parentId)
    {
      ParentId = parentId;
    }
  }
}