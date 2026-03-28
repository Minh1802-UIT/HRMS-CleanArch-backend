using MongoDB.Bson.Serialization.Attributes;
using Employee.Domain.Entities.Common;
using Employee.Domain.Enums;

namespace Employee.Domain.Entities.Simulation;

/// <summary>
/// Represents a virtual employee bot that autonomously performs HR system actions
/// such as check-in, check-out, and leave requests on a daily schedule.
/// </summary>
public class SimulationBot : BaseEntity
{
    [BsonElement("employeeId")]
    public string EmployeeId { get; set; } = string.Empty;

    [BsonElement("employeeCode")]
    public string EmployeeCode { get; set; } = string.Empty;

    [BsonElement("employeeName")]
    public string EmployeeName { get; set; } = string.Empty;

    /// <summary>
    /// Bot personality / work pattern for deterministic-random behaviour.
    /// </summary>
    [BsonElement("activityProfile")]
    public SimulationActivityProfile ActivityProfile { get; set; } = new();

    [BsonElement("status")]
    public SimulationBotStatus Status { get; set; } = SimulationBotStatus.Active;

    [BsonElement("lastSimulatedAt")]
    public DateTime? LastSimulatedAt { get; set; }

    [BsonElement("consecutiveFailures")]
    public int ConsecutiveFailures { get; set; }

    [BsonElement("totalSimulationsRun")]
    public int TotalSimulationsRun { get; set; }

    /// <summary>
    /// UTC+7 window start for daily check-in (default 08:00 = 1:00 UTC).
    /// </summary>
    public TimeSpan CheckInWindowStartUtc { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// UTC+7 window end for daily check-in (default 09:00 = 2:00 UTC).
    /// </summary>
    public TimeSpan CheckInWindowEndUtc { get; set; } = TimeSpan.FromHours(2);

    /// <summary>
    /// UTC+7 shift length in hours (default 8h).
    /// </summary>
    public double ShiftHours { get; set; } = 8.0;

    /// <summary>
    /// Day of week patterns: which days the bot normally works.
    /// </summary>
    public List<DayOfWeek> WorkDays { get; set; } = new()
    {
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
        DayOfWeek.Thursday, DayOfWeek.Friday
    };

    public SimulationBot() { }

    public SimulationBot(string employeeId, string employeeCode, string employeeName)
    {
        EmployeeId = employeeId;
        EmployeeCode = employeeCode;
        EmployeeName = employeeName;
        CreatedAt = DateTime.UtcNow;
    }

    public void MarkSimulationRun(DateTime simulatedAt)
    {
        LastSimulatedAt = simulatedAt;
        TotalSimulationsRun++;
        ConsecutiveFailures = 0;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkSimulationFailure()
    {
        ConsecutiveFailures++;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsWorkDay(DateTime utcDate)
    {
        var localDate = utcDate.AddHours(7);
        return WorkDays.Contains(localDate.DayOfWeek);
    }
}

/// <summary>
/// Personality weights that drive stochastic simulation behaviour.
/// </summary>
public class SimulationActivityProfile
{
    /// <summary>Weighted name used to pick from predefined personalities.</summary>
    [BsonElement("profileName")]
    public string ProfileName { get; set; } = "Regular";

    /// <summary>0.0–1.0 — probability the bot checks in on time (vs late).</summary>
    [BsonElement("onTimeProbability")]
    public double OnTimeProbability { get; set; } = 0.85;

    /// <summary>0.0–1.0 — probability the bot submits a leave request this month.</summary>
    [BsonElement("monthlyLeaveProbability")]
    public double MonthlyLeaveProbability { get; set; } = 0.15;

    /// <summary>0.0–1.0 — probability the bot checks in (vs skips the day).</summary>
    [BsonElement("attendanceProbability")]
    public double AttendanceProbability { get; set; } = 0.98;

    /// <summary>Minutes early (negative) or late (positive) from standard check-in time.</summary>
    [BsonElement("checkInVarianceMinutes")]
    public int CheckInVarianceMinutes { get; set; } = 15;

    /// <summary>How often the bot works on Saturday (WFH culture). Default 10%.</summary>
    [BsonElement("saturdayWorkProbability")]
    public double SaturdayWorkProbability { get; set; } = 0.10;

    /// <summary>Maximum overtime hours the bot may claim per day.</summary>
    [BsonElement("maxOvertimeHoursPerDay")]
    public double MaxOvertimeHoursPerDay { get; set; } = 2.0;
}

public enum SimulationBotStatus
{
    Active,
    Paused,
    Error
}
