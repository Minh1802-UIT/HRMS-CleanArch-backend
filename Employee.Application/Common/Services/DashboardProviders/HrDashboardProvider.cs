using Employee.Application.Common.Dtos;
using Employee.Application.Common.Interfaces.Organization.IRepository;
using Employee.Application.Common.Interfaces.Organization.IService;
using Employee.Application.Common.Models;
using Employee.Domain.Entities.HumanResource;
using System.Linq;

namespace Employee.Application.Common.Services.DashboardProviders
{
  public class HrDashboardProvider : IDashboardProvider
  {
    private readonly IEmployeeRepository _employeeRepo;
    private readonly IDepartmentRepository _deptRepo;

    public HrDashboardProvider(IEmployeeRepository employeeRepo, IDepartmentRepository deptRepo)
    {
      _employeeRepo = employeeRepo;
      _deptRepo = deptRepo;
    }

    public async Task PopulateDashboardAsync(DashboardDto dto)
    {
      var totalEmployeesCount = await _employeeRepo.CountActiveAsync();

      dto.SummaryCards.Add(new SummaryCardDto
      {
        Title = "Total Employees",
        Value = totalEmployeesCount.ToString(),
        Icon = "group",
        ColorScheme = "blue"
      });

      var recentHires = await _employeeRepo.GetRecentHiresAsync(10);
      var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

      dto.RecentHires = recentHires
        .Where(e => e.JobDetails != null && e.JobDetails.JoinDate >= startOfMonth)
        .OrderByDescending(e => e.JobDetails.JoinDate)
        .Take(5)
        .Select(e => new NewHireDto
        {
          Name = e.FullName,
          Initials = GetInitials(e.FullName),
          Position = "Staff", // Simplified for now
          JoinDate = e.JobDetails.JoinDate,
          ColorScheme = GetColor(e.Id.GetHashCode())
        }).ToList();

      // Calculation of Staff Distribution by Department
      var allActiveEmployees = await _employeeRepo.GetAllActiveAsync();
      var allActiveDepts = await _deptRepo.GetAllActiveAsync();

      var distribution = allActiveEmployees
        .Where(e => e.JobDetails != null && !string.IsNullOrEmpty(e.JobDetails.DepartmentId))
        .GroupBy(e => e.JobDetails.DepartmentId)
        .Select(g => new
        {
          DeptId = g.Key,
          Count = g.Count()
        })
        .ToList();

      foreach (var item in distribution)
      {
        var deptName = allActiveDepts.FirstOrDefault(d => d.Id == item.DeptId)?.Name ?? "Others";
        dto.Analytics.StaffDistribution.Labels.Add(deptName);
        dto.Analytics.StaffDistribution.Data.Add(item.Count);
      }
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
