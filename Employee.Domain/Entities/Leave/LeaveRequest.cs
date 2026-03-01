using Employee.Domain.Entities.Common;
using Employee.Domain.Enums;

namespace Employee.Domain.Entities.Leave
{
    public class LeaveRequest : BaseEntity
    {
        public string EmployeeId { get; private set; } = null!; // Automatically taken from Token
        public LeaveCategory LeaveType { get; private set; } // Annual, Sick, Unpaid
        public DateTime FromDate { get; private set; }
        public DateTime ToDate { get; private set; }
        public string Reason { get; private set; } = null!;
        public LeaveStatus Status { get; private set; }
        public string? ManagerComment { get; private set; } // Refusal reason or manager note
        public string? ApprovedBy { get; private set; } // ID of the approver (null if not yet approved)

        // Constructor for EF Core / Serialization
        private LeaveRequest() { }

        // Factory Constructor
        public LeaveRequest(string employeeId, LeaveCategory leaveType, DateTime fromDate, DateTime toDate, string reason)
        {
            if (string.IsNullOrWhiteSpace(employeeId)) throw new ArgumentException("EmployeeId is required.");
            if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Reason is required.");
            if (reason.Length > 300) throw new ArgumentException("Reason must not exceed 300 characters.");
            if (toDate < fromDate) throw new ArgumentException("ToDate must be greater than or equal to FromDate.");

            EmployeeId = employeeId;
            LeaveType = leaveType;
            FromDate = fromDate;
            ToDate = toDate;
            Reason = reason;
            Status = LeaveStatus.Pending; // Default status
        }

        // Domain Methods
        public void Approve(string approvedBy, string? comment)
        {
            if (Status != LeaveStatus.Pending)
                throw new InvalidOperationException($"Cannot approve leave request in '{Status}' status.");

            Status = LeaveStatus.Approved;
            ApprovedBy = approvedBy;
            ManagerComment = comment;
        }

        public void Reject(string rejectedBy, string comment)
        {
            if (Status != LeaveStatus.Pending)
                throw new InvalidOperationException($"Cannot reject leave request in '{Status}' status.");

            Status = LeaveStatus.Rejected;
            ApprovedBy = rejectedBy; // Stores who rejected
            ManagerComment = comment;
        }

        public void Cancel(DateTime cancelledAt)
        {
            if (Status == LeaveStatus.Approved)
            {
                // Domain Event: LeaveCancelled (to trigger refund) could be raised here
            }
            Status = LeaveStatus.Cancelled;
            SetUpdatedAt(cancelledAt);
        }

    public void Update(LeaveCategory leaveType, DateTime fromDate, DateTime toDate, string reason, DateTime updatedAt)
        {
            if (Status != LeaveStatus.Pending)
            {
                throw new InvalidOperationException("Leave request can only be updated when in Pending status.");
            }
            if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Reason is required.");
            if (reason.Length > 300) throw new ArgumentException("Reason must not exceed 300 characters.");
            if (toDate < fromDate) throw new ArgumentException("ToDate must be greater than or equal to FromDate.");

            LeaveType = leaveType;
            FromDate = fromDate;
            ToDate = toDate;
            Reason = reason;
            SetUpdatedAt(updatedAt);
        }
    }
}
