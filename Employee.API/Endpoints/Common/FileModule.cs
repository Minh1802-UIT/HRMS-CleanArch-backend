using Carter;
using Employee.API.Common;

namespace Employee.API.Endpoints.Common
{
  public class FileModule : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      var group = app.MapGroup("/api/files")
                     .WithTags("Common - Files")
                     .RequireAuthorization(); // Optional: only authenticated users can upload

      group.MapPost("/upload", FileHandlers.UploadFile)
           .DisableAntiforgery() // Essential for Multipart Form Data in Minimal APIs if global filters are present
           .RequireRateLimiting("file-upload"); // 20 uploads/hour per user
    }
  }
}
