using System.ComponentModel.DataAnnotations;

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
        [Required(ErrorMessage = "Leave Type is required.")]
        public string LeaveType { get; set; } = "Annual";

        [Required(ErrorMessage = "From Date is required.")]
        public DateTime FromDate { get; set; }

        [Required(ErrorMessage = "To Date is required.")]
        public DateTime ToDate { get; set; }

        [Required(ErrorMessage = "Reason is required.")]
        [MaxLength(500, ErrorMessage = "Reason must not exceed 500 characters.")]
        public string Reason { get; set; } = string.Empty;
    }

    // ==========================================
    // UPDATE (Input - Employee edits request)
    // ==========================================
    public class UpdateLeaveRequestDto : CreateLeaveRequestDto
    {
        [Required]
        public string Id { get; set; } = string.Empty;
    }

    // ==========================================
    // REVIEW (Input - Manager approves/rejects)
    // ==========================================
    public class ReviewLeaveRequestDto
    {
        [Required]
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "Status is required.")]
        [RegularExpression("^(Approved|Rejected)$", ErrorMessage = "Status must be 'Approved' or 'Rejected'.")]
        public string Status { get; set; } = "Approved";

        [MaxLength(500, ErrorMessage = "Manager Comment must not exceed 500 characters.")]
        public string? ManagerComment { get; set; }
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