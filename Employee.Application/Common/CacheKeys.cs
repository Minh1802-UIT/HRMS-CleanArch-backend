namespace Employee.Application.Common
{
    /// <summary>
    /// Centralized cache key constants to avoid magic strings scattered across handlers.
    /// </summary>
    public static class CacheKeys
    {
        public const string DepartmentTree = "DEPARTMENT_TREE";
        public const string PositionTree = "POSITION_TREE";
        public const string EmployeeLookup = "EMPLOYEE_LOOKUP";
        public const string DashboardStats = "DASHBOARD_STATS";

        /// <summary>
        /// Per-entity cache with ID suffix. Example: "EMPLOYEE:abc123"
        /// </summary>
        public static string Employee(string id) => $"EMPLOYEE:{id}";
        public static string Department(string id) => $"DEPARTMENT:{id}";
        public static string Position(string id) => $"POSITION:{id}";
    }
}
