using Employee.Domain.Entities;

namespace Employee.Domain.Entities.Common
{
    public class SystemSetting : BaseEntity
    {
    public string Key { get; private set; } = string.Empty;
    public string Value { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Group { get; private set; } = string.Empty; // e.g., "Payroll", "Attendance"

    private SystemSetting() { }

    public SystemSetting(string key, string group, string value = "", string description = "")
    {
      Key = key;
      Group = group;
      Value = value;
      Description = description;
    }

    public void UpdateValue(string value, string description)
    {
      Value = value;
      Description = description;
      SetUpdatedAt(DateTime.UtcNow);
    }
    }
}
