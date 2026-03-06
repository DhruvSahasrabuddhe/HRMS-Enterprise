namespace HRMS.Shared.Constants
{
    /// <summary>
    /// Application-wide constants to avoid magic strings and numbers.
    /// </summary>
    public static class HrmsConstants
    {
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

        public static class Roles
        {
            public const string Admin = "Admin";
            public const string HR = "HR";
            public const string Manager = "Manager";
            public const string Employee = "Employee";
        }

        public static class Pagination
        {
            public const int DefaultPageSize = 10;
            public const int MaxPageSize = 100;
            public const int DefaultPageNumber = 1;
        }

        public static class LeaveEntitlement
        {
            public const int AnnualLeaveDays = 21;
            public const int SickLeaveDays = 14;
            public const int MaternityLeaveDays = 90;
            public const int PaternityLeaveDays = 14;
            public const int BereavementLeaveDays = 5;
            public const int MarriageLeaveDays = 5;
        }

        public static class Employee
        {
            public const string CodePrefix = "EMP";
            public const int CodePaddingLength = 5;
        }
    }
}
