using Employee.Domain.Enums;
using System;

namespace Employee.Domain.Entities.ValueObjects
{
    /// <summary>
    /// Embedded document for employee job assignment.
    /// NOTE: This is NOT a pure Value Object — it contains foreign-key references
    /// (DepartmentId, PositionId, ManagerId, ShiftId) that link to other aggregates.
    /// It is modelled as a record (immutable once set) and stored as a nested
    /// MongoDB sub-document for query convenience.
    /// Future refactoring may promote these IDs to the EmployeeEntity root level.
    /// </summary>
    public record JobDetails
    {
        // ── Aggregate References (NOT value-object fields) ─────────────────
        public string DepartmentId { get; init; } = string.Empty;
        public string PositionId { get; init; } = string.Empty;
        public string ManagerId { get; init; } = string.Empty;
        public string ShiftId { get; init; } = string.Empty;

        // ── Pure value fields ──────────────────────────────────────────────
        public DateTime JoinDate { get; init; }
        public EmployeeStatus Status { get; init; } = EmployeeStatus.Probation;
        public string? ResumeUrl { get; init; }
        public string? ContractUrl { get; init; }
        public DateTime? ProbationEndDate { get; init; }
    }
}
