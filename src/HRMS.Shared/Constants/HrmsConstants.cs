namespace HRMS.Shared.Constants
{
    /// <summary>
    /// Application-wide constants to avoid magic strings and numbers.
    /// </summary>
    public static class HrmsConstants
    {
        /// <summary>
        /// Caching-related constants for cache keys and expiration times.
        /// </summary>
        public static class Cache
        {
            public const int DefaultExpirationMinutes = 5;
            public const string EmployeeListKey = "employees_list";
            public const string AllDepartmentsKey = "all_departments";
            public const string AllLeavesKey = "all_leaves";
            
            public static string EmployeeKey(int id) => $"employee_{id}";
            public static string DepartmentKey(int id) => $"department_{id}";
            public static string LeaveKey(int id) => $"leave_{id}";
            public static string EmployeeLeavesKey(int employeeId) => $"employee_leaves_{employeeId}";
        }

        /// <summary>
        /// User role constants for authorization.
        /// </summary>
        public static class Roles
        {
            public const string Admin = "Admin";
            public const string HR = "HR";
            public const string Manager = "Manager";
            public const string Employee = "Employee";
        }

        /// <summary>
        /// Pagination defaults and limits.
        /// </summary>
        public static class Pagination
        {
            public const int DefaultPageSize = 10;
            public const int MaxPageSize = 100;
            public const int DefaultPageNumber = 1;
        }

        /// <summary>
        /// Default leave entitlement days by leave type.
        /// </summary>
        public static class LeaveEntitlement
        {
            public const int AnnualLeaveDays = 21;
            public const int SickLeaveDays = 14;
            public const int MaternityLeaveDays = 90;
            public const int PaternityLeaveDays = 14;
            public const int BereavementLeaveDays = 5;
            public const int MarriageLeaveDays = 5;
        }

        /// <summary>
        /// Employee-related constants.
        /// </summary>
        public static class Employee
        {
            public const string CodePrefix = "EMP";
            public const int CodePaddingLength = 5;
            public const int CodeYearStartIndex = 3;
            public const int CodeYearLength = 4;
        }

        /// <summary>
        /// Dashboard and reporting constants.
        /// </summary>
        public static class Dashboard
        {
            public const int RecentActivityDays = 30;
            public const int RecentActivityLimit = 10;
            public const int RecentActivityHiresLimit = 5;
            public const int RecentActivityLeavesLimit = 5;
            public const int UpcomingLeavesDays = 7;
            public const int ChartMonthsToShow = 6;
        }

        /// <summary>
        /// UI-related constants for charts and colors.
        /// </summary>
        public static class UI
        {
            public const string PrimaryChartColor = "#3b7cff";
            public const string SuccessColor = "#28a745";
            public const string DangerColor = "#dc3545";
            public const string WarningColor = "#ffc107";
            public const string InfoColor = "#17a2b8";
        }

        /// <summary>
        /// Activity type constants for dashboard activities.
        /// </summary>
        public static class ActivityTypes
        {
            public const string NewHire = "New Hire";
            public const string LeaveRequest = "Leave Request";
            public const string EmployeeUpdate = "Employee Update";
            public const string DepartmentChange = "Department Change";
        }

        /// <summary>
        /// Date and time calculation constants.
        /// </summary>
        public static class DateCalculations
        {
            public const int DaysInWeek = 7;
            public const int MonthsInYear = 12;
            public const int WorkingDaysPerWeek = 5;
        }
    }
}
