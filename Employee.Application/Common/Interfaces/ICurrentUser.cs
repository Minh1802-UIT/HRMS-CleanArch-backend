using System.Security.Claims;

namespace Employee.Application.Common.Interfaces
{
  public interface ICurrentUser
  {
    string UserId { get; }
    string? EmployeeId { get; }
    string? UserName { get; }
    bool IsInRole(string role);
  }
}