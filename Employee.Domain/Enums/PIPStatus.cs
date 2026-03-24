namespace Employee.Domain.Enums
{
  /// <summary>
  /// Represents the lifecycle status of a Performance Improvement Plan.
  /// </summary>
  public enum PIPStatus
  {
    /// <summary>Plan has been created but not yet started.</summary>
    Draft = 0,

    /// <summary>Plan is actively being executed.</summary>
    InProgress = 1,

    /// <summary>Employee successfully completed all improvement objectives.</summary>
    Completed = 2,

    /// <summary>Employee failed to meet objectives within the plan period.</summary>
    Failed = 3,

    /// <summary>Plan was cancelled before completion.</summary>
    Cancelled = 4
  }
}
