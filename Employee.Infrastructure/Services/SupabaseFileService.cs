using Employee.Application.Common.Dtos;
using Employee.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Supabase.Storage;
using System.Text.RegularExpressions;

namespace Employee.Infrastructure.Services
{
    /// <summary>
    /// Cloud-based file storage using Supabase Storage.
    /// Replaces the local FileService so uploads persist across deployments.
    /// </summary>
    public class SupabaseFileService : IFileService
    {
        private readonly SupabaseStorageOptions _options;
        private readonly Supabase.Storage.Client _storageClient;
        private readonly ILogger<SupabaseFileService> _logger;

        private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".pdf", ".docx" };

        public SupabaseFileService(
            IOptions<SupabaseStorageOptions> options,
            ILogger<SupabaseFileService> logger)
        {
            _options = options.Value;
            _logger = logger;

            // Storage REST API lives at {ProjectUrl}/storage/v1
            var storageUrl = $"{_options.ProjectUrl.TrimEnd('/')}/storage/v1";

            var headers = new Dictionary<string, string>
            {
                { "Authorization", $"Bearer {_options.ServiceKey}" },
                { "apikey", _options.ServiceKey }
            };

            _storageClient = new Supabase.Storage.Client(storageUrl, headers);
        }

        public async Task<string> UploadFileAsync(FileUploadRequest file, string folderName)
        {
            if (file == null || file.Length == 0)
                return string.Empty;

            // ── 1. Validation ──────────────────────────────────────
            if (file.Length > MaxFileSizeBytes)
                throw new InvalidOperationException(
                    $"File size exceeds the {MaxFileSizeBytes / (1024 * 1024)} MB limit.");

            var extension = Path.GetExtension(file.FileName).ToLower();
            if (!AllowedExtensions.Contains(extension))
                throw new InvalidOperationException(
                    "Invalid file type. Only images, PDFs, and DOCX are allowed.");

            // ── 2. Sanitize ────────────────────────────────────────
            folderName = Regex.Replace(folderName, @"[^a-zA-Z0-9_\-]", "");
            var safeOriginal = Regex.Replace(
                Path.GetFileNameWithoutExtension(file.FileName),
                @"[^a-zA-Z0-9_\-]", "");
            var fileName = $"{Guid.NewGuid()}_{safeOriginal}{extension}";
            var supabasePath = $"{folderName}/{fileName}";

            // ── 3. Upload to Supabase ──────────────────────────────
            using var ms = new MemoryStream();
            await file.Content.CopyToAsync(ms);
            var data = ms.ToArray();

            var bucket = _storageClient.From(_options.BucketName);
            await bucket.Upload(
                data,
                supabasePath,
                new Supabase.Storage.FileOptions
                {
                    ContentType = file.ContentType ?? "application/octet-stream",
                    Upsert = false
                });

            _logger.LogInformation("File uploaded to Supabase: {Path}", supabasePath);

            // ── 4. Return public URL ───────────────────────────────
            var publicUrl = bucket.GetPublicUrl(supabasePath);
            return publicUrl;
        }
    }
}
