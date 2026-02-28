using Employee.Application.Common.Dtos;
using Employee.Application.Common.Interfaces;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace Employee.Infrastructure.Services
{
    public class FileService : IFileService
    {
        private readonly FileStorageOptions _options;
        private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5MB limit
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".pdf", ".docx" };

        public FileService(IOptions<FileStorageOptions> options)
        {
            _options = options.Value;
        }

        public async Task<string> UploadFileAsync(FileUploadRequest file, string folderName)
        {
            if (file == null || file.Length == 0)
                return string.Empty;

            // 1. VALIDATION
            // 1.1 Size check
            if (file.Length > MaxFileSizeBytes)
                throw new InvalidOperationException($"File size exceeds limit of {MaxFileSizeBytes / (1024 * 1024)}MB.");

            // 1.2 Extension check
            var extension = Path.GetExtension(file.FileName).ToLower();
            if (!AllowedExtensions.Contains(extension))
                throw new InvalidOperationException("Invalid file extension. Only images, PDFs, and DOCX are allowed.");

            // 1.3 Path Traversal Guard (Sanitize folderName)
            folderName = Regex.Replace(folderName, @"[^a-zA-Z0-9_\-]", ""); // Only allow safe characters

            // 2. PATH SETUP
            var baseDir = string.IsNullOrWhiteSpace(_options.BaseDirectory)
                          ? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads")
                          : Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", _options.BaseDirectory));
            var uploadsBase = Path.GetFullPath(baseDir);
            var uploadsFolder = Path.Combine(uploadsBase, folderName);

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // 3. SECURE FILENAME
            var safeOriginalName = Regex.Replace(Path.GetFileNameWithoutExtension(file.FileName), @"[^a-zA-Z0-9_\-]", "");
            var fileName = $"{Guid.NewGuid()}_{safeOriginalName}{extension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            // Double check the path is still within the expected base directory
            if (!Path.GetFullPath(filePath).StartsWith(Path.GetFullPath(uploadsBase)))
                throw new InvalidOperationException("Invalid upload path detected.");

            // 4. SAVE
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.Content.CopyToAsync(stream);
            }

            // 5. RETURN RELATIVE PATH
            return $"/uploads/{folderName}/{fileName}";
        }
    }
}
