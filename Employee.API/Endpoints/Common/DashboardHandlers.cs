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
            return Results.Ok(new { Succeeded = true, Message = "Lấy dữ liệu Dashboard thành công", Data = data });
        }
    }
}
