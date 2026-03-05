using System.Text.Json.Serialization;

namespace Employee.Application.Common.Wrappers
{
  public class ApiResponse<T>
  {
    [JsonPropertyName("succeeded")]
    public bool Succeeded { get; set; }

    // Machine-readable error code for client-side i18n (e.g. "EMP_NOT_FOUND")
    [JsonPropertyName("errorCode")]
    public string ErrorCode { get; set; } = string.Empty;

    // Human-readable description for logs and debug output
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public T? Data { get; set; }

    // Validation error details, if any
    [JsonPropertyName("errors")]
    public List<string>? Errors { get; set; }

    // Required for deserialization
    public ApiResponse() { }

    // Success constructor
    public ApiResponse(T data, string message = "Success")
    {
      Succeeded = true;
      Data = data;
      Message = message;
      ErrorCode = string.Empty;
      Errors = null;
    }

    // Failure constructor
    public ApiResponse(string errorCode, string devMessage, List<string>? errors = null)
    {
      Succeeded = false;
      ErrorCode = errorCode;
      Message = devMessage;
      Errors = errors;
      Data = default;
    }

    // Static factory helpers
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