using Employee.Application.Common.Dtos;

namespace Employee.Application.Common.Interfaces.Organization.IService
{
    public interface IDashboardService
    {
        Task<DashboardDto> GetDashboardDataAsync();
    }
}
