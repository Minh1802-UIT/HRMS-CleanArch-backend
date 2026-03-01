
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
        public string Name { get; set; } = string.Empty;
        public int DefaultDays { get; set; }

        public string? Description { get; set; }
    }

    // ==========================================
    // UPDATE (Input)
    // ==========================================
    public class UpdateLeaveTypeDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int DefaultDays { get; set; }

        public string? Description { get; set; }
    }
}