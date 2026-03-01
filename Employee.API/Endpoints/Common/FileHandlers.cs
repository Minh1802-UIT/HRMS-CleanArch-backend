using Employee.API.Common;
using Employee.Application.Common.Dtos;
using Employee.Application.Common.Interfaces;
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
        return ResultUtils.Fail("FILE_NOT_UPLOADED", "No file uploaded.", 400);

      // 1. Validate File Ext
      var ext = Path.GetExtension(file.FileName).ToLower();
      var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf", ".doc", ".docx" };
      if (!allowedExtensions.Contains(ext))
        return ResultUtils.Fail("FILE_TYPE_NOT_ALLOWED", $"File type '{ext}' is not allowed.", 400);

      // 2. Max Size (5MB)
      if (file.Length > 5 * 1024 * 1024)
        return ResultUtils.Fail("FILE_SIZE_EXCEEDED", "File size exceeds the 5 MB limit.", 400);

      // 3. Map IFormFile → FileUploadRequest
      var uploadRequest = new FileUploadRequest
      {
        Content = file.OpenReadStream(),
        FileName = file.FileName,
        ContentType = file.ContentType,
        Length = file.Length
      };

      try
      {
        var path = await fileService.UploadFileAsync(uploadRequest, folderName);
        return ResultUtils.Success(path, "File uploaded successfully.");
      }
      catch (InvalidOperationException ex)
      {
        return ResultUtils.Fail("FILE_UPLOAD_ERROR", ex.Message, 400);
      }
      catch (Exception ex)
      {
        return ResultUtils.Fail("FILE_UPLOAD_ERROR",
            $"Upload failed: {ex.Message}", 500);
      }
    }
  }
}
