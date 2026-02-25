using Employee.Application.Common.Dtos;

namespace Employee.Application.Common.Interfaces
{
    public interface IFileService
    {
        Task<string> UploadFileAsync(FileUploadRequest file, string folderName);
    }
}