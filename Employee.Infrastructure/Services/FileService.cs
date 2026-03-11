using Employee.Application.Common.Dtos;
using Employee.Application.Common.Interfaces;
using Microsoft.Extensions.Options;
using Supabase.Storage.Interfaces;
using System.Text.RegularExpressions;

namespace Employee.Infrastructure.Services
{
    public class FileService : IFileService
    {
        private readonly SupabaseStorageOptions _options;
        private readonly Supabase.Client _supabaseClient;
        private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5MB limit
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".pdf", ".docx" };

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

        public FileService(IOptions<SupabaseStorageOptions> options, Supabase.Client supabaseClient)
        {
            _options = options.Value;
            _supabaseClient = supabaseClient;
        }

        public async Task<string> UploadFileAsync(FileUploadRequest file, string folderName)
        {
            if (file == null || file.Length == 0)
                return string.Empty;

            // 1. VALIDATION
            if (file.Length > MaxFileSizeBytes)
                throw new InvalidOperationException($"File size exceeds limit of {MaxFileSizeBytes / (1024 * 1024)}MB.");

            var extension = Path.GetExtension(file.FileName).ToLower();
            if (!AllowedExtensions.Contains(extension))
                throw new InvalidOperationException("Invalid file extension. Only images, PDFs, and DOCX are allowed.");

            using var peekStream = new MemoryStream();
            await file.Content.CopyToAsync(peekStream);
            var fileBytes = peekStream.ToArray();
            
            if (!HasValidMagicBytes(fileBytes, extension))
                throw new InvalidOperationException("File content does not match the declared file type.");

            folderName = Regex.Replace(folderName, @"[^a-zA-Z0-9_\-]", ""); 

            // 2. SECURE FILENAME
            var safeOriginalName = Regex.Replace(Path.GetFileNameWithoutExtension(file.FileName), @"[^a-zA-Z0-9_\-]", "");
            var fileName = $"{Guid.NewGuid()}_{safeOriginalName}{extension}";
            var supabasePath = $"{folderName}/{fileName}";

            // 3. UPLOAD TO SUPABASE
            var storage = _supabaseClient.Storage.From(_options.BucketName);
            var fileOptions = new Supabase.Storage.FileOptions { ContentType = file.ContentType ?? "application/octet-stream" };
            await storage.Upload(fileBytes, supabasePath, fileOptions);

            // 4. RETURN PUBLIC URL
            return storage.GetPublicUrl(supabasePath);
        }
    }
}
