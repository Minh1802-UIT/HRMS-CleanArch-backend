namespace Employee.Application.Common.Models;

public class LookupDto
{
  public string Id { get; set; } = string.Empty;
  public string Label { get; set; } = string.Empty;
  public string? SecondaryLabel { get; set; }
}
