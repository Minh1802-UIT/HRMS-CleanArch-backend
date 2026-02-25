using Employee.Application.Common.Dtos;
using Employee.Application.Common.Interfaces;
using Employee.Application.Common.Wrappers;
using Microsoft.AspNetCore.Mvc;

namespace Employee.API.Endpoints.Common
{
  public static class FileHandlers
  {
    public static async Task<IResult> UploadFile(
        HttpContext context,
        IFileService fileService,
        [FromForm] string folderName = "general")
    {
      var form = await context.Request.ReadFormAsync();
      var file = form.Files.GetFile("file");

      if (file == null || file.Length == 0)
      {
        return Results.BadRequest(new ApiResponse<string>("FILE_NOT_UPLOADED", "No file uploaded"));
      }

      // 1. Validate File Ext (Basic)
      var ext = Path.GetExtension(file.FileName).ToLower();
      var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf", ".doc", ".docx" };
      if (!allowedExtensions.Contains(ext))
      {
        return Results.BadRequest(new ApiResponse<string>("FILE_TYPE_NOT_ALLOWED", "File type not allowed"));
      }

      // 2. Max Size (e.g., 5MB)
      if (file.Length > 5 * 1024 * 1024)
      {
        return Results.BadRequest(new ApiResponse<string>("FILE_SIZE_EXCEEDED", "File size exceeds 5MB limit"));
      }

      // 3. Map IFormFile → FileUploadRequest (framework-agnostic DTO)
      var uploadRequest = new FileUploadRequest
      {
        Content = file.OpenReadStream(),
        FileName = file.FileName,
        ContentType = file.ContentType,
        Length = file.Length
      };

      // 4. Save via Service
      var path = await fileService.UploadFileAsync(uploadRequest, folderName);

      return Results.Ok(new ApiResponse<string>(path, "File uploaded successfully"));
    }
  }
}
