
namespace Employee.Application.Features.Leave.Dtos
{
    // ==========================================
    // VIEW (Output)
    // ==========================================
    public class LeaveRequestDto
    {
        public string Id { get; set; } = string.Empty;
        public string EmployeeId { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string EmployeeCode { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;

        public string LeaveType { get; set; } = string.Empty;
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public double TotalDays { get; set; }

        public string Reason { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // Pending, Approved, Rejected

        public string? ManagerComment { get; set; }
        public string? ApprovedBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ==========================================
    // CREATE (Input)
    // ==========================================
    public class CreateLeaveRequestDto
    {
        public string LeaveType { get; set; } = "Annual";
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    // ==========================================
    // UPDATE (Input - Employee edits request)
    // ==========================================
    public class UpdateLeaveRequestDto : CreateLeaveRequestDto
    {
        public string Id { get; set; } = string.Empty;
    }

    // ==========================================
    // REVIEW (Input - Manager approves/rejects)
    // ==========================================
    public class ReviewLeaveRequestDto
    {
        public string Id { get; set; } = string.Empty;
        public string Status { get; set; } = "Approved";
        public string? ManagerComment { get; set; }
        /// <summary>
        /// Optimistic concurrency version. Client must send the version from the GET response.
        /// Server rejects with 409 if another user modified the leave request concurrently.
        /// </summary>
        public int ExpectedVersion { get; set; } = 1;
    }

    // ==========================================
    // FILTER (Input - Search/Filter list)
    // ==========================================
    public class LeaveRequestFilterDto
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? Status { get; set; }
        public string? LeaveType { get; set; }
    }
}