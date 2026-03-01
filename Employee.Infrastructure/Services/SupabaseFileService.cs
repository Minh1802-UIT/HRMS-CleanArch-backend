using Employee.Application.Common.Dtos;
using Employee.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace Employee.Infrastructure.Services
{
    /// <summary>
    /// Cloud-based file storage using Supabase Storage REST API (HttpClient).
    /// Replaces the local FileService so uploads persist across deployments.
    /// </summary>
    public class SupabaseFileService : IFileService
    {
        private readonly SupabaseStorageOptions _options;
        private readonly HttpClient _http;
        private readonly ILogger<SupabaseFileService> _logger;
        private readonly string _storageBaseUrl;

        private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB
        private static readonly string[] AllowedExtensions =
            { ".jpg", ".jpeg", ".png", ".pdf", ".docx" };

        public SupabaseFileService(
            IOptions<SupabaseStorageOptions> options,
            IHttpClientFactory httpClientFactory,
            ILogger<SupabaseFileService> logger)
        {
            _options = options.Value;
            _logger = logger;

            // ── Validate configuration ─────────────────────────────
            if (string.IsNullOrWhiteSpace(_options.ProjectUrl)
                || _options.ProjectUrl.Contains("OVERRIDE", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError("SupabaseStorage:ProjectUrl is not configured.");
                throw new InvalidOperationException(
                    "Supabase Storage is not configured. Missing ProjectUrl.");
            }

            if (string.IsNullOrWhiteSpace(_options.ServiceKey)
                || _options.ServiceKey.Contains("OVERRIDE", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError("SupabaseStorage:ServiceKey is not configured.");
                throw new InvalidOperationException(
                    "Supabase Storage is not configured. Missing ServiceKey.");
            }

            _storageBaseUrl = $"{_options.ProjectUrl.TrimEnd('/')}/storage/v1";
            _logger.LogInformation("Supabase Storage base URL: {Url}", _storageBaseUrl);

            _http = httpClientFactory.CreateClient("SupabaseStorage");
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _options.ServiceKey);
            _http.DefaultRequestHeaders.Add("apikey", _options.ServiceKey);
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
            var objectPath = $"{folderName}/{fileName}";

            // ── 3. Upload via REST API ─────────────────────────────
            // POST /storage/v1/object/{bucket}/{path}
            var uploadUrl = $"{_storageBaseUrl}/object/{_options.BucketName}/{objectPath}";

            try
            {
                using var ms = new MemoryStream();
                await file.Content.CopyToAsync(ms);
                var data = ms.ToArray();

                _logger.LogInformation(
                    "Uploading to Supabase: url={Url}, size={Size} bytes",
                    uploadUrl, data.Length);

                using var content = new ByteArrayContent(data);
                content.Headers.ContentType =
                    new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");

                var response = await _http.PostAsync(uploadUrl, content);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "Supabase upload failed: status={Status}, body={Body}",
                        (int)response.StatusCode, body);
                    throw new InvalidOperationException(
                        $"Supabase upload returned {(int)response.StatusCode}: {body}");
                }

                _logger.LogInformation("File uploaded successfully: {Path}", objectPath);

                // ── 4. Build public URL ────────────────────────────
                var publicUrl =
                    $"{_storageBaseUrl}/object/public/{_options.BucketName}/{objectPath}";

                _logger.LogInformation("Public URL: {Url}", publicUrl);
                return publicUrl;
            }
            catch (InvalidOperationException)
            {
                throw; // re-throw our own errors
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Supabase upload exception: url={Url}", uploadUrl);
                throw new InvalidOperationException(
                    $"Failed to upload file to cloud storage: {ex.Message}", ex);
            }
        }
    }
}
