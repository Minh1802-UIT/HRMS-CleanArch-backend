namespace Employee.Domain.Constants
{
  public static class ErrorCodes
  {
    // -----------------------------------------------------
    // PART 1: GENERAL ERROR CODES
    // -----------------------------------------------------
    public const string InternalError = "SYS_INTERNAL_ERROR";
    public const string Unauthorized = "AUTH_UNAUTHORIZED";
    public const string Forbidden = "AUTH_FORBIDDEN";
    public const string InvalidData = "VAL_INVALID_INPUT";
    public const string SystemLocked = "SYS_LOCKED";
    public const string UnlinkedAccount = "AUTH_UNLINKED_ACCOUNT";

    // -----------------------------------------------------
    // PART 2: GENERIC ERROR CODES
    // -----------------------------------------------------

    /// <summary>
    /// Creates a Not Found error code. E.g., NotFound("EMP") => "EMP_NOT_FOUND"
    /// </summary>
    public static string NotFound(string prefix) => $"{prefix.ToUpper()}_NOT_FOUND";

    /// <summary>
    /// Creates a Conflict error code. E.g., Conflict("EMP_CODE") => "EMP_CODE_EXIST"
    /// </summary>
    public static string Conflict(string prefix) => $"{prefix.ToUpper()}_EXIST";

    /// <summary>
    /// Creates a Date Conflict error code. E.g., "CONTRACT_DATE_CONFLICT"
    /// </summary>
    public static string DateConflict(string prefix) => $"{prefix.ToUpper()}_DATE_CONFLICT";

    /// <summary>
    /// Creates a Locked error code. E.g., "PAYROLL_LOCKED"
    /// </summary>
    public static string Locked(string prefix) => $"{prefix.ToUpper()}_LOCKED";

    /// <summary>
    /// Creates an Invalid error code. E.g., Invalid("EMAIL") => "EMAIL_INVALID" (400)
    /// </summary>
    public static string Invalid(string prefix) => $"{prefix.ToUpper()}_INVALID";

    /// <summary>
    /// Creates a Missing error code. E.g., Missing("NAME") => "NAME_MISSING" (400)
    /// </summary>
    public static string Missing(string prefix) => $"{prefix.ToUpper()}_MISSING";

    /// <summary>
    /// Creates a Failed error code. E.g., "PAYMENT_FAILED" (400)
    /// </summary>
    public static string Failed(string prefix) => $"{prefix.ToUpper()}_FAILED";
  }
}