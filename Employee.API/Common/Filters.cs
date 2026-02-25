using MiniValidation;

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
        if (!MiniValidator.TryValidate(arg, out var errors))
        {
            // Trả về 400 ngay lập tức, KHÔNG cho chạy tiếp vào Handler
            return Results.ValidationProblem(errors);
        }

        // 3. Nếu OK thì cho đi tiếp vào Handler
        return await next(context);
    }
}