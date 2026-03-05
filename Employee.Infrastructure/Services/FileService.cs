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

        // SECURITY: Magic bytes (file signatures) prevent attackers from renaming
        // malicious files to an allowed extension (e.g. shell.php → shell.jpg).
        private static readonly Dictionary<string, byte[][]> MagicBytes = new()
        {
            { ".jpg",  [ [0xFF, 0xD8, 0xFF] ] },
            { ".jpeg", [ [0xFF, 0xD8, 0xFF] ] },
            { ".png",  [ [0x89, 0x50, 0x4E, 0x47] ] },
            { ".pdf",  [ [0x25, 0x50, 0x44, 0x46] ] },
            { ".docx", [ [0x50, 0x4B, 0x03, 0x04], [0x50, 0x4B, 0x05, 0x06] ] },
        };

        private static bool HasValidMagicBytes(byte[] fileBytes, string extension)
        {
            if (!MagicBytes.TryGetValue(extension, out var signatures))
                return false;
            return signatures.Any(sig =>
                fileBytes.Length >= sig.Length &&
                fileBytes.Take(sig.Length).SequenceEqual(sig));
        }

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

            // 1.3 Magic bytes check — validate file content matches the declared extension
            using var peekStream = new MemoryStream();
            await file.Content.CopyToAsync(peekStream);
            var fileBytes = peekStream.ToArray();
            if (!HasValidMagicBytes(fileBytes, extension))
                throw new InvalidOperationException("File content does not match the declared file type.");

            // 1.4 Path Traversal Guard (Sanitize folderName)
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

            // 4. SAVE (use already-read fileBytes to avoid re-reading the stream)
            await System.IO.File.WriteAllBytesAsync(filePath, fileBytes);

            // 5. RETURN RELATIVE PATH
            return $"/uploads/{folderName}/{fileName}";
        }
    }
}
