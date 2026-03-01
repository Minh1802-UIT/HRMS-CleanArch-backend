namespace Employee.Domain.Common.Models;

/// <summary>
/// Lightweight projection for PayrollProcessing - only contains employee IDs
/// </summary>
public class ActiveEmployeeProjection
{
  public string Id { get; set; } = string.Empty;
}
