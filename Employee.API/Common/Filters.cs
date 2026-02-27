using MiniValidation;
using Employee.Application.Common.Wrappers;

namespace Employee.API.Common;

// Filter này nhận vào 1 Type T (ví dụ CreateDepartmentDto)
public class ValidationFilter<T> : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        // 1. Tìm object T trong danh sách tham số gửi lên
        var arg = context.Arguments.FirstOrDefault(x => x?.GetType() == typeof(T));

        if (arg is null) return Results.BadRequest("Invalid arguments");

        // 2. Validate bằng MiniValidator
        if (!MiniValidator.TryValidate(arg, out var validationErrors))
        {
            // Trả về 400 với cùng định dạng ApiResponse<T> để nhất quán với toàn bộ API
            var errorMessages = validationErrors
                .SelectMany(kvp => kvp.Value.Select(msg => $"{kvp.Key}: {msg}"))
                .ToList();
            var response = ApiResponse<object?>.FailResult("VALIDATION_ERROR", "Dữ liệu không hợp lệ.", errorMessages);
            return Results.Json(response, statusCode: 400);
        }

        // 3. Nếu OK thì cho đi tiếp vào Handler
        return await next(context);
    }
}