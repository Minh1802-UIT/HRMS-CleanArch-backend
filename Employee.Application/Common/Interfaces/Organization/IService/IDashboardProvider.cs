using Employee.Application.Common.Dtos;

namespace Employee.Application.Common.Interfaces.Organization.IService
{
  public interface IDashboardProvider
  {
    Task PopulateDashboardAsync(DashboardDto dto);
  }
}
