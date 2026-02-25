using AspNetCore.Identity.MongoDbCore.Models;
using System;

namespace Employee.Infrastructure.Identity.Models
{
  public class ApplicationUser : MongoIdentityUser<Guid>
  {
    public string FullName { get; set; } = string.Empty;
    public string? EmployeeId { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }
    public bool IsActive { get; set; } = true;
    public bool MustChangePassword { get; set; } = false;
  }
}
