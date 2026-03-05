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
        private readonly string _serviceKey;

        private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB
        private static readonly string[] AllowedExtensions =
            { ".jpg", ".jpeg", ".png", ".pdf", ".docx" };

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

        public SupabaseFileService(
            IOptions<SupabaseStorageOptions> options,
            IHttpClientFactory httpClientFactory,
            ILogger<SupabaseFileService> logger)
        {
            _options = options.Value;
            _logger = logger;

            // ── Validate configuration ─────────────────────────────
            var projectUrl = _options.ProjectUrl?.Trim() ?? "";
            var serviceKey = _options.ServiceKey?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(projectUrl)
                || projectUrl.Contains("OVERRIDE", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError("SupabaseStorage:ProjectUrl is not configured.");
                throw new InvalidOperationException(
                    "Supabase Storage is not configured. Missing ProjectUrl.");
            }

            if (string.IsNullOrWhiteSpace(serviceKey)
                || serviceKey.Contains("OVERRIDE", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError("SupabaseStorage:ServiceKey is not configured.");
                throw new InvalidOperationException(
                    "Supabase Storage is not configured. Missing ServiceKey.");
            }

            // Strip quotes if env var was wrapped in them
            serviceKey = serviceKey.Trim('"', '\'');

            _serviceKey = serviceKey;
            _storageBaseUrl = $"{projectUrl.TrimEnd('/')}/storage/v1";

            _logger.LogInformation("Supabase Storage base URL: {Url}", _storageBaseUrl);
            // SECURITY: Never log credentials or partial key material — removed

            _http = httpClientFactory.CreateClient("SupabaseStorage");
            // Do NOT set DefaultRequestHeaders — use per-request headers instead
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

                // SECURITY: Magic bytes check — validate file content matches declared extension.
                // Extension-only checks can be bypassed by renaming a malicious file.
                if (!HasValidMagicBytes(data, extension))
                    throw new InvalidOperationException("File content does not match the declared file type.");

                _logger.LogInformation(
                    "Uploading to Supabase: url={Url}, size={Size} bytes",
                    uploadUrl, data.Length);

                using var content = new ByteArrayContent(data);
                content.Headers.ContentType =
                    new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");

                // Per-request headers — avoids DefaultRequestHeaders issues
                using var request = new HttpRequestMessage(HttpMethod.Post, uploadUrl);
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", _serviceKey);
                request.Headers.Add("apikey", _serviceKey);
                request.Content = content;

                var response = await _http.SendAsync(request);
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
