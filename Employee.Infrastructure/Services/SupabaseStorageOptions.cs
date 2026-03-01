namespace Employee.Infrastructure.Services
{
    public class SupabaseStorageOptions
    {
        public const string SectionName = "SupabaseStorage";
        public string ProjectUrl { get; set; } = string.Empty;
        public string ServiceKey { get; set; } = string.Empty;
        public string BucketName { get; set; } = "employee-files";
    }
}
