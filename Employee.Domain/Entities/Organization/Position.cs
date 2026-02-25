using Employee.Domain.Entities.Common;
using Employee.Domain.Entities.ValueObjects;
using System;

namespace Employee.Domain.Entities.Organization
{
  public class Position : BaseEntity
  {
    public string Title { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;

    // Embedded Value Object
    public SalaryRange SalaryRange { get; private set; } = new();

    public string DepartmentId { get; private set; } = string.Empty;
    public string? ParentId { get; private set; } // Direct superior position

    private Position() { }

    public Position(string title, string code, string departmentId)
    {
      if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title is required.");
      if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Code is required.");
      if (string.IsNullOrWhiteSpace(departmentId)) throw new ArgumentException("DepartmentId is required.");

      Title = title;
      Code = code;
      DepartmentId = departmentId;
    }

    public void UpdateInfo(string title, SalaryRange salaryRange)
    {
      if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title is required.");
      Title = title;
      SalaryRange = salaryRange ?? throw new ArgumentNullException(nameof(salaryRange));
    }

    public void SetParent(string? parentId)
    {
      ParentId = parentId;
    }

    public void UpdateSalaryRange(SalaryRange range)
    {
      SalaryRange = range ?? throw new ArgumentNullException(nameof(range));
    }

    public void ChangeDepartment(string departmentId)
    {
      DepartmentId = departmentId;
    }
  }
}