namespace Employee.Application.Common.Utils
{
    public static class DateHelper
    {
        /// <summary>
        /// Count working days (Mon-Fri) between two dates, inclusive.
        /// Used for standard leave requests.
        /// </summary>
        public static double CountWorkingDays(DateTime start, DateTime end)
        {
            double days = 0;
            for (var date = start.Date; date <= end.Date; date = date.AddDays(1))
            {
                if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                    days++;
            }
            return days > 0 ? days : 0;
        }

        /// <summary>
        /// Count ALL calendar days between two dates, inclusive (Sandwich Rule).
        /// When a leave type has IsSandwichRuleApplied = true, weekends between
        /// leave days are counted as consumed leave days.
        /// Example: Friday–Monday = 4 days (not 2 working days).
        /// </summary>
        public static double CountCalendarDays(DateTime start, DateTime end)
        {
            var days = (end.Date - start.Date).TotalDays + 1;
            return days > 0 ? days : 0;
        }
    }
}
