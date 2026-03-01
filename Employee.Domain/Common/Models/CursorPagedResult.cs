namespace Employee.Domain.Common.Models;

/// <summary>
/// Result of a cursor-based (keyset / seek) paginated query.
/// Avoids the O(N) skip cost of offset pagination for deep pages.
///
/// Usage:
///   First page  → send AfterCursor = null
///   Next page   → send AfterCursor = result.NextCursor
///   Last page   → result.NextCursor is null
/// </summary>
public class CursorPagedResult<T>
{
  public List<T> Items { get; set; } = new();

  /// <summary>
  /// Opaque string to pass as 'afterCursor' for the next page.
  /// Null when this is the last page.
  /// </summary>
  public string? NextCursor { get; set; }

  public bool HasNextPage => NextCursor is not null;
  public int PageSize { get; set; }
}
