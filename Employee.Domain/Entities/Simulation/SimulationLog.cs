using Employee.Domain.Entities.Common;
using Employee.Domain.Enums;

namespace Employee.Domain.Entities.Simulation;

/// <summary>
/// A completed simulation run record — written once per action type per bot per day.
/// Used for analytics, debugging, and audit trail.
/// </summary>
public class SimulationLog : BaseEntity
{
    public string BotId { get; set; } = string.Empty;
    public string EmployeeId { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;

    /// <summary>
    /// The simulated date (UTC, date part only).
    /// </summary>
    public DateTime SimulatedDateUtc { get; set; }

    /// <summary>
    /// UTC timestamp when the simulation action was performed.
    /// </summary>
    public DateTime SimulatedAtUtc { get; set; }

    /// <summary>
    /// What kind of action was performed.
    /// </summary>
    public SimulationActionType ActionType { get; set; }

    /// <summary>
    /// Human-readable description of the action (e.g. "Checked in at 08:03 ICT").
    /// </summary>
    public string ActionDescription { get; set; } = string.Empty;

    /// <summary>
    /// Whether the action succeeded or failed.
    /// </summary>
    public SimulationLogResult Result { get; set; }

    /// <summary>
    /// Error message if Result is Failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The target entity ID created/modified by this action
    /// (e.g. leave request ID, attendance log ID).
    /// </summary>
    public string? TargetEntityId { get; set; }

    /// <summary>
    /// For API calls: HTTP status code returned by the endpoint.
    /// </summary>
    public int? HttpStatusCode { get; set; }

    /// <summary>
    /// Simulation duration in milliseconds.
    /// </summary>
    public double DurationMs { get; set; }

    public SimulationLog() { }

    public static SimulationLog Create(
        string botId, string employeeId, string employeeName,
        SimulationActionType actionType, string description,
        SimulationLogResult result, DateTime simulatedAt)
    {
        return new SimulationLog
        {
            BotId = botId,
            EmployeeId = employeeId,
            EmployeeName = employeeName,
            SimulatedDateUtc = simulatedAt.Date,
            SimulatedAtUtc = simulatedAt,
            ActionType = actionType,
            ActionDescription = description,
            Result = result,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkSuccess(string? targetEntityId, int httpStatusCode, double durationMs)
    {
        Result = SimulationLogResult.Success;
        TargetEntityId = targetEntityId;
        HttpStatusCode = httpStatusCode;
        DurationMs = durationMs;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkFailure(string errorMessage, int? httpStatusCode, double durationMs)
    {
        Result = SimulationLogResult.Failed;
        ErrorMessage = errorMessage;
        HttpStatusCode = httpStatusCode;
        DurationMs = durationMs;
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum SimulationActionType
{
    CheckIn,
    CheckOut,
    LeaveRequest,
    GoalUpdate,
    PerformanceReview,
    DataCreation
}

public enum SimulationLogResult
{
    Success,
    Failed,
    Skipped
}
