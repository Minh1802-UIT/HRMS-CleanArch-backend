using Employee.Domain.Entities.Common;
using System;

namespace Employee.Domain.Entities.Attendance
{
    /// <summary>
    /// Stores a face embedding (128-float vector) for an employee.
    /// Employee self-registers; HR/Admin must approve before it is used for verification.
    /// Only one active embedding per employee is allowed (no glasses).
    /// </summary>
    public class FaceEmbedding : BaseEntity
    {
        public string EmployeeId { get; set; } = string.Empty;

        /// <summary>128-dimensional face descriptor vector from face-api.js</summary>
        public float[] Embedding { get; set; } = Array.Empty<float>();

        /// <summary>Small JPEG base64 thumbnail of the registered face (~80x80)</summary>
        public string? PhotoThumbnail { get; set; }

        /// <summary>Status: Pending → Approved / Rejected by HR/Admin</summary>
        public FaceRegistrationStatus Status { get; set; } = FaceRegistrationStatus.Pending;

        /// <summary>Who approved/rejected (userId)</summary>
        public string? ReviewedBy { get; set; }

        /// <summary>Review timestamp</summary>
        public DateTime? ReviewedAt { get; set; }

        /// <summary>Rejection reason (if rejected)</summary>
        public string? RejectionReason { get; set; }

        // Parameterless ctor for MongoDB
        public FaceEmbedding() { }

        public FaceEmbedding(string employeeId, float[] embedding, string? photoThumbnail = null)
        {
            if (string.IsNullOrWhiteSpace(employeeId))
                throw new ArgumentException("EmployeeId is required.");
            if (embedding.Length != 128)
                throw new ArgumentException("Embedding must be 128-dimensional.");

            EmployeeId = employeeId;
            Embedding = embedding;
            PhotoThumbnail = photoThumbnail;
            Status = FaceRegistrationStatus.Pending;
            CreatedAt = DateTime.UtcNow;
        }

        public void Approve(string reviewerId)
        {
            Status = FaceRegistrationStatus.Approved;
            ReviewedBy = reviewerId;
            ReviewedAt = DateTime.UtcNow;
        }

        public void Reject(string reviewerId, string? reason = null)
        {
            Status = FaceRegistrationStatus.Rejected;
            ReviewedBy = reviewerId;
            ReviewedAt = DateTime.UtcNow;
            RejectionReason = reason;
        }
    }

    public enum FaceRegistrationStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2
    }
}
