namespace Employee.Application.Common.Dtos;

/// <summary>
/// Generic key-label pair used for dropdown / autocomplete queries.
/// Moved from Employee.Domain.Common.Models to Application layer — DTOs are an
/// Application/Presentation concern and must not pollute the Domain model.
/// </summary>
public class LookupDto
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? SecondaryLabel { get; set; }
}
