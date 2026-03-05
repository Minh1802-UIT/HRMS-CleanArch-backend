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
    Holiday
  }
}
