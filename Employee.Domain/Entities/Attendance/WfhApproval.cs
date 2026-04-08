using Employee.Domain.Entities.Common;
using System;

namespace Employee.Domain.Entities.Attendance
{
    /// <summary>
    /// Pre-approval record for Work From Home.
    /// Admin creates these; the verification service checks for an active WFH approval 
    /// when an employee selects a Remote check-in point.
    /// </summary>
    public class WfhApproval : BaseEntity
    {
        public string EmployeeId { get; set; } = string.Empty;

        /// <summary>Start date of the WFH approval period.</summary>
        public DateTime FromDate { get; set; }

        /// <summary>End date of the WFH approval period (inclusive).</summary>
        public DateTime ToDate { get; set; }

        public string? Reason { get; set; }

        /// <summary>Who approved this WFH request.</summary>
        public string ApprovedBy { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        // Parameterless ctor for MongoDB
        public WfhApproval() { }

        public WfhApproval(string employeeId, DateTime fromDate, DateTime toDate, string approvedBy, string? reason = null)
        {
            if (string.IsNullOrWhiteSpace(employeeId)) throw new ArgumentException("EmployeeId is required.");
            EmployeeId = employeeId;
            FromDate = fromDate.Date;
            ToDate = toDate.Date;
            ApprovedBy = approvedBy;
            Reason = reason;
            CreatedAt = DateTime.UtcNow;
        }

        public bool CoversDate(DateTime date) => date.Date >= FromDate.Date && date.Date <= ToDate.Date;
    }
}
