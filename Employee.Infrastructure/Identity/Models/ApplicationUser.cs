using AspNetCore.Identity.MongoDbCore.Models;
using System;

namespace Employee.Infrastructure.Identity.Models
{
  public class ApplicationUser : MongoIdentityUser<Guid>
  {
    public string FullName { get; set; } = string.Empty;
    public string? EmployeeId { get; set; }

    /// <summary>
    /// All active and recently-revoked refresh-token entries for this user.
    /// Each entry stores a SHA-256 hash (never the raw token) and belongs to
    /// a family grouping one login session's rotations for reuse detection.
    /// </summary>
    public List<RefreshTokenEntry> RefreshTokens { get; set; } = new();

    public bool IsActive { get; set; } = true;
    public bool MustChangePassword { get; set; } = false;
  }
}
