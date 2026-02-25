using Employee.Application.Common.Wrappers;

namespace Employee.API.Common
{
  public static class ResultUtils
  {
    // 1. Success có Data
    public static IResult Success<T>(T data, string message = "Success")
    {
      return Results.Ok(ApiResponse<T>.SuccessResult(data, message));
    }

    // 2. Success không Data (VD: Delete xong)
    public static IResult Success(string message = "Success")
    {
      // Sử dụng SuccessResult với data là null
      return Results.Ok(ApiResponse<object?>.SuccessResult(null, message));
    }

    // 3. Fail (Quan trọng)
    public static IResult Fail(string errorCode, string devMessage, int? statusCode = null, List<string>? errors = null)
    {
      var response = ApiResponse<object?>.FailResult(errorCode, devMessage, errors);

      // Bước 1: Nếu Handler truyền status code cụ thể thì dùng luôn (Ưu tiên cao nhất)
      // Bước 2: Nếu không truyền, tự suy luận dựa trên QUY TẮC ĐẶT TÊN của errorCode
      int finalStatusCode = statusCode ?? GetStatusCodeByConvention(errorCode);

      return Results.Json(response, statusCode: finalStatusCode);
    }

    // 🔥 LOGIC THÔNG MINH Ở ĐÂY 🔥
    // Không quan tâm Employee hay Payroll, chỉ quan tâm "Loại lỗi" là gì
    public static int GetStatusCodeByConvention(string errorCode)
    {
      if (string.IsNullOrEmpty(errorCode)) return 400;

      var code = errorCode.ToUpper(); // Chuẩn hóa về chữ hoa để so sánh

      // 1. Nhóm lỗi 404 (Không tìm thấy)
      // Quy ước: Mã lỗi kết thúc bằng _NOT_FOUND
      // Ví dụ: EMP_NOT_FOUND, CONTRACT_NOT_FOUND, PAYROLL_NOT_FOUND...
      if (code.EndsWith("_NOT_FOUND")) return 404;

      // 2. Nhóm lỗi 409 (Xung đột/Trùng lặp)
      // Quy ước: Kết thúc bằng _EXIST, _CONFLICT, _DUPLICATE
      // Ví dụ: EMP_CODE_EXIST, SHIFT_CONFLICT...
      if (code.EndsWith("_EXIST") || code.EndsWith("_CONFLICT") || code.EndsWith("_DUPLICATE")) return 409;

      // 3. Nhóm lỗi 401 (Chưa đăng nhập)
      if (code.EndsWith("_UNAUTHORIZED") || code.EndsWith("_REQUIRED")) return 401;

      // 4. Nhóm lỗi 403 (Không đủ quyền)
      if (code.EndsWith("_FORBIDDEN") || code.EndsWith("_DENIED")) return 403;

      // 5. Nhóm lỗi 500 (Lỗi server)
      if (code.EndsWith("_INTERNAL_ERROR") || code.EndsWith("_SERVER_ERROR")) return 500;

      // 👉 6. (MỚI) Nhóm 400 - Bad Request / Validation Error
      // Quy ước: Lỗi dữ liệu đầu vào, sai định dạng, thiếu trường...
      if (code.EndsWith("_INVALID") ||      // VD: EMAIL_INVALID, ID_INVALID
          code.EndsWith("_MISSING") ||      // VD: NAME_MISSING
          code.EndsWith("_FAILED") ||       // VD: ACTION_FAILED
          code.EndsWith("_BAD_REQUEST"))    // VD: GENERIC_BAD_REQUEST
      {
        return 400;
      }

      // 7. Mặc định vẫn là 400 cho các trường hợp không định nghĩa
      return 400;
    }

    // 4. Created (201)
    public static IResult Created<T>(T data, string message = "Created successfully")
    {
      return Results.Json(ApiResponse<T>.SuccessResult(data, message), statusCode: 201);
    }

    public static IResult Created(string message = "Created successfully")
    {
      // Truyền <object?> và data = null một cách tường minh
      return Created<object?>(null, message);
    }
  }
}