namespace Employee.Application.Common.Dtos
{
    /// <summary>
    /// Framework-agnostic file upload abstraction.
    /// The API layer maps IFormFile → FileUploadRequest before calling Application services.
    /// </summary>
    public class FileUploadRequest
    {
        public required Stream Content { get; init; }
        public required string FileName { get; init; }
        public required string ContentType { get; init; }
        public long Length { get; init; }
    }
}
