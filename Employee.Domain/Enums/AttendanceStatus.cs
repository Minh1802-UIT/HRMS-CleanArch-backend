namespace Employee.Domain.Enums
{
  /// <summary>
  /// Base attendance status — describes whether an employee was present or absent.
  /// Fine-grained violations (Late, EarlyLeave, MissingPunch) are captured as
  /// boolean flags on DailyLog so that combined violations are representable.
  /// </summary>
  public enum AttendanceStatus
  {
    Absent,
    Present,
    Leave,
    Holiday,

    /// <summary>Legacy value — stored in old MongoDB documents before the boolean-flag refactor.
    /// New records use Status=Present + IsLate=true. Do NOT use in new code.</summary>
    [System.Obsolete("Use Status=Present with IsLate=true instead.")]
    Late,

    /// <summary>Legacy value — stored in old MongoDB documents before the boolean-flag refactor.
    /// New records use Status=Present + IsEarlyLeave=true. Do NOT use in new code.</summary>
    [System.Obsolete("Use Status=Present with IsEarlyLeave=true instead.")]
    EarlyLeave
  }
}
