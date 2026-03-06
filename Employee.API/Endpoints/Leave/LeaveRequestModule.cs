using Carter;
using Employee.API.Common;
using Employee.Application.Features.Leave.Dtos;

namespace Employee.API.Endpoints.Leave
{
    public class LeaveRequestModule : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/leaves")
                           .WithTags("Leave Management - Requests")
                           .RequireAuthorization(); // Bắt buộc đăng nhập

               // -----------------------------------------------------------
               // A. EMPLOYEE ROUTES (Nhân viên sử dụng)
               // -----------------------------------------------------------

               group.MapGet("/", LeaveRequestHandlers.GetPagedList)
                    .WithName("GetPagedLeaveRequestList")
                 .RequireAuthorization(p => p.RequireRole("Admin", "HR", "Manager"));

            // 1. Lấy danh sách của tôi
            group.MapGet("/me", LeaveRequestHandlers.GetMyLeaves);

            // 2. Lấy chi tiết đơn
            group.MapGet("/{id}", LeaveRequestHandlers.GetById);

            // 2.5 Lấy lịch sử nghỉ phép của một nhân viên (Admin/HR/Manager)
            group.MapGet("/employee/{employeeId}", LeaveRequestHandlers.GetByEmployeeId)
                 .RequireAuthorization(p => p.RequireRole("Admin", "HR", "Manager"));

            // 3. Tạo đơn (Có Validate đầu vào)
            group.MapPost("/", LeaveRequestHandlers.Create)
                 .AddEndpointFilter<ValidationFilter<CreateLeaveRequestDto>>()
                 .RequireRateLimiting("write");   // 30 mutations/min per user

               // 4. Sửa đơn
               group.MapPatch("/{id}", LeaveRequestHandlers.Update)
                    .AddEndpointFilter<ValidationFilter<UpdateLeaveRequestDto>>()
                 .RequireRateLimiting("write");

               // 5. Hủy đơn
               group.MapPost("/{id}/cancel", LeaveRequestHandlers.Cancel)
                    .RequireRateLimiting("write");


               // -----------------------------------------------------------
               // B. MANAGER ROUTES (Quản lý sử dụng)
               // -----------------------------------------------------------

               // 6. Duyệt đơn (Chỉ Admin/HR/Manager mới được gọi)
               group.MapPost("/{id}/review", LeaveRequestHandlers.Review)
                    .AddEndpointFilter<ValidationFilter<ReviewLeaveRequestDto>>()
                 .RequireAuthorization(p => p.RequireRole("Admin", "HR", "Manager"));
        }
    }
}