namespace Employee.Infrastructure.Services
{
    public class FileStorageOptions
    {
        public const string SectionName = "FileStorage";
        public string BaseDirectory { get; set; } = "uploads";
    }
}
