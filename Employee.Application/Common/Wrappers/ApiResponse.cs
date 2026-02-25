namespace Employee.Application.Common.Wrappers
{
  public class ApiResponse<T>
  {
    public bool Succeeded { get; set; }

    // Dành cho Frontend: Dùng mã này để map ra tiếng Việt/Anh (VD: "EMP_NOT_FOUND")
    public string ErrorCode { get; set; } = string.Empty;

    // Dành cho Developer/Log: Mô tả chi tiết lỗi tiếng Anh (VD: "Employee with id 123 not found")
    public string Message { get; set; } = string.Empty;

    public T? Data { get; set; }

    // Dành cho Validation: List các lỗi chi tiết (VD: ["Email invalid", "Age must be > 18"])
    public List<string>? Errors { get; set; }

    // Constructor mặc định (bắt buộc để Serialization hoạt động)
    public ApiResponse() { }

    // ✅ Constructor 1: THÀNH CÔNG (Success)
    public ApiResponse(T data, string message = "Success")
    {
      Succeeded = true;
      Data = data;
      Message = message;
      ErrorCode = string.Empty; // Thành công thì không có mã lỗi
      Errors = null;
    }

    // ❌ Constructor 2: THẤT BẠI (Failure)
    public ApiResponse(string errorCode, string devMessage, List<string>? errors = null)
    {
      Succeeded = false;
      ErrorCode = errorCode;
      Message = devMessage;
      Errors = errors;
      Data = default;
    }

    // 🌟 STATIC FACTORY METHODS (Để tránh nhập nhằng constructor)
    public static ApiResponse<T> SuccessResult(T data, string message = "Success")
    {
      return new ApiResponse<T>(data, message);
    }

    public static ApiResponse<T> FailResult(string errorCode, string message, List<string>? errors = null)
    {
      return new ApiResponse<T>(errorCode, message, errors);
    }
  }
}