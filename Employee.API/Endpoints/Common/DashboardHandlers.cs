using Employee.API.Common;
using Employee.Application.Common.Interfaces.Organization.IService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Employee.API.Endpoints.Common
{
    public static class DashboardHandlers
    {
        public static async Task<IResult> GetDashboardData(IDashboardService service)
        {
            var data = await service.GetDashboardDataAsync();
            return ResultUtils.Success(data, "Dashboard data retrieved successfully.");
        }
    }
}
