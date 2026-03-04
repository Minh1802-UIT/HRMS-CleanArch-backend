namespace Employee.Domain.Common.Models;

public class PaginationParams
{
    public int? PageNumber { get; set; } = 1;

    // Cap at 500 to allow payroll pages which may have many employees
    private int? _pageSize = 20;
    public int? PageSize
    {
        get => _pageSize;
        set => _pageSize = value.HasValue ? Math.Min(Math.Max(value.Value, 1), 500) : null;
    }

    public string? SortBy { get; set; }
    public bool? IsDescending { get; set; } = false;
    public string? SearchTerm { get; set; }
    public string? DepartmentId { get; set; }
    public string? PositionId { get; set; }
}
