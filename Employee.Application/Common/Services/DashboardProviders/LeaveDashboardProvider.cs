using Employee.Application.Common.Dtos;
using Employee.Application.Common.Interfaces.Organization.IRepository;
using Employee.Application.Common.Interfaces.Organization.IService;
using Employee.Application.Common.Models;
using Employee.Domain.Entities.HumanResource;
using Employee.Domain.Entities.Leave;
using System.Linq;

namespace Employee.Application.Common.Services.DashboardProviders
{
  public class LeaveDashboardProvider : IDashboardProvider
  {
    private readonly ILeaveRequestRepository _leaveRepo;
    private readonly IEmployeeRepository _employeeRepo;

    public LeaveDashboardProvider(ILeaveRequestRepository leaveRepo, IEmployeeRepository employeeRepo)
    {
      _leaveRepo = leaveRepo;
      _employeeRepo = employeeRepo;
    }

    public async Task PopulateDashboardAsync(DashboardDto dto)
    {
      var pendingLeavesCount = await _leaveRepo.CountByStatusAsync(Employee.Domain.Enums.LeaveStatus.Pending);

      dto.SummaryCards.Add(new SummaryCardDto
      {
        Title = "Pending Leaves",
        Value = pendingLeavesCount.ToString(),
        Icon = "clock",
        ColorScheme = "orange"
      });

      var recentLeaves = await _leaveRepo.GetRecentAsync(10);
      var pendingLeavesList = recentLeaves.Where(l => l.Status == Employee.Domain.Enums.LeaveStatus.Pending).Take(5).ToList();

      var employees = await _employeeRepo.GetAllActiveAsync();

      dto.PendingRequests = pendingLeavesList
        .Select(l =>
        {
          var emp = employees.FirstOrDefault(e => e.Id == l.EmployeeId);
          return new PendingRequestDto
          {
            EmployeeName = emp?.FullName ?? "Unknown",
            Initials = GetInitials(emp?.FullName ?? "U"),
            DateRange = $"{l.FromDate:MMM dd} - {l.ToDate:MMM dd}",
            Type = l.LeaveType.ToString(),
            ColorScheme = GetColor(l.Id.GetHashCode())
          };
        }).ToList();
    }

    private string GetInitials(string name)
    {
      if (string.IsNullOrWhiteSpace(name)) return "U";
      var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
      if (parts.Length == 0) return "U";
      if (parts.Length == 1) return parts[0].Length > 0 ? parts[0][0].ToString().ToUpper() : "U";
      return (parts[0][0].ToString() + parts[parts.Length - 1][0].ToString()).ToUpper();
    }

    private string GetColor(int seed)
    {
      var colors = new[] { "blue", "green", "purple", "orange", "pink", "teal" };
      return colors[Math.Abs(seed) % colors.Length];
    }
  }
}
