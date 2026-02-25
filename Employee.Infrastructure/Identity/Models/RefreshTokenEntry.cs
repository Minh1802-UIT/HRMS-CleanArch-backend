namespace Employee.Infrastructure.Identity.Models
{
  /// <summary>
  /// One issued refresh-token stored per-user (embedded in ApplicationUser).
  /// Each entry belongs to a <see cref="FamilyId"/> that groups all rotations
  /// originating from the same login session.  Re-use of a revoked token within
  /// the same family indicates token theft → the entire family is revoked.
  /// </summary>
  public class RefreshTokenEntry
  {
    /// <summary>SHA-256 hash of the raw token sent in the cookie.</summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>Stable identifier shared by every rotation in one login session.</summary>
    public string FamilyId { get; set; } = string.Empty;

    public DateTime IssuedAt { get; set; }
    public DateTime ExpiresAt { get; set; }

    /// <summary>True once the token has been exchanged for a new one, or revoked explicitly.</summary>
    public bool IsRevoked { get; set; }
  }
}
