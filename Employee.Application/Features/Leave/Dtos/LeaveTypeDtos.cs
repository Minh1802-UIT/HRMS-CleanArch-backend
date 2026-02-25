using System.ComponentModel.DataAnnotations;

namespace Employee.Application.Features.Leave.Dtos
{
    // ==========================================
    // VIEW (Output)
    // ==========================================
    public class LeaveTypeDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public int DefaultDays { get; set; }
        public string? Description { get; set; }
    }

    // ==========================================
    // CREATE (Input)
    // ==========================================
    public class CreateLeaveTypeDto
    {
        [Required(ErrorMessage = "Name is required.")]
        public string Name { get; set; } = string.Empty;

        [Range(0, 365, ErrorMessage = "Default days must be between 0 and 365.")]
        public int DefaultDays { get; set; }

        public string? Description { get; set; }
    }

    // ==========================================
    // UPDATE (Input)
    // ==========================================
    public class UpdateLeaveTypeDto
    {
        [Required]
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Range(0, 365, ErrorMessage = "Default days must be between 0 and 365.")]
        public int DefaultDays { get; set; }

        public string? Description { get; set; }
    }
}