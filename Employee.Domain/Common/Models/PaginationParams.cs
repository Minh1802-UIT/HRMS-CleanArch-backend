namespace Employee.Domain.Common.Models;

public class PaginationParams
{
    public int? PageNumber { get; set; } = 1;

    // Cap at 100 to prevent clients from requesting the entire collection in one call
    // and causing memory exhaustion / DoS.
    private int? _pageSize = 20;
    public int? PageSize
    {
        get => _pageSize;
        set => _pageSize = value.HasValue ? Math.Min(Math.Max(value.Value, 1), 100) : null;
    }

    public string? SortBy { get; set; }
    public bool? IsDescending { get; set; } = false;
    public string? SearchTerm { get; set; }
    public string? DepartmentId { get; set; }
    public string? PositionId { get; set; }
}
