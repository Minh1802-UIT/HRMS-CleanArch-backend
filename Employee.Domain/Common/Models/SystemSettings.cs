namespace Employee.Domain.Common.Models
{
    public class SystemSettings
    {
        /// <summary>
        /// IANA timezone ID (e.g., "Asia/Ho_Chi_Minh") or Windows timezone ID.
        /// Replaces the old hardcoded TimezoneOffsetHours = 7.
        /// </summary>
        public string TimezoneId { get; set; } = "Asia/Ho_Chi_Minh";
    }
}
