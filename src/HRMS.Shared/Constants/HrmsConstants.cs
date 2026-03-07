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
            /// <summary>Default absolute expiration for cached items (minutes).</summary>
            public const int DefaultExpirationMinutes = 5;

            /// <summary>Sliding expiration for frequently-accessed items (minutes).</summary>
            public const int SlidingExpirationMinutes = 2;

            /// <summary>Longer absolute expiration for reference data that rarely changes (minutes).</summary>
            public const int LongExpirationMinutes = 30;

            // List / collection cache keys
            public const string EmployeeListKey = "employees_list";
            public const string AllDepartmentsKey = "all_departments";
            public const string AllLeavesKey = "all_leaves";
            public const string AllNotificationsKey = "all_notifications";

            // Per-entity cache-key factories
            public static string EmployeeKey(int id) => $"employee_{id}";
            public static string DepartmentKey(int id) => $"department_{id}";
            public static string LeaveKey(int id) => $"leave_{id}";
            public static string EmployeeLeavesKey(int employeeId) => $"employee_leaves_{employeeId}";
            public static string AttendanceKey(int id) => $"attendance_{id}";
            public static string EmployeeAttendanceKey(int employeeId) => $"employee_attendance_{employeeId}";
        }

        /// <summary>
        /// Database connection and resilience constants.
        /// </summary>
        public static class Database
        {
            /// <summary>Maximum number of transient-failure retries for SQL connections.</summary>
            public const int MaxRetryCount = 5;

            /// <summary>Maximum delay (seconds) between retry attempts.</summary>
            public const int MaxRetryDelaySeconds = 30;

            /// <summary>Default command timeout in seconds.</summary>
            public const int CommandTimeoutSeconds = 30;

            /// <summary>Maximum number of statements batched in a single round-trip.</summary>
            public const int MaxBatchSize = 100;
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

        /// <summary>
        /// Security-related constants for password policies, encryption, and rate limiting.
        /// </summary>
        public static class Security
        {
            // Password Policy
            public const int MinPasswordLength = 12;
            public const int MaxPasswordLength = 100;
            public const bool RequireDigit = true;
            public const bool RequireLowercase = true;
            public const bool RequireUppercase = true;
            public const bool RequireNonAlphanumeric = true;
            public const int RequiredUniqueChars = 4;

            // Account Lockout
            public const int MaxFailedAccessAttempts = 5;
            public const int LockoutDurationMinutes = 15;
            public const bool AllowedForNewUsers = true;

            // Rate Limiting
            public const int GeneralRateLimitRequests = 100;
            public const int GeneralRateLimitPeriodSeconds = 60;
            public const int AuthRateLimitRequests = 5;
            public const int AuthRateLimitPeriodSeconds = 60;

            // Encryption
            public const string EncryptionKeyConfigName = "Encryption:Key";
            public const string EncryptionIVConfigName = "Encryption:IV";

            // Data Masking
            public const char MaskCharacter = '*';
            public const int UnmaskedPrefixLength = 2;
            public const int UnmaskedSuffixLength = 2;

            // Security Headers
            public const string XFrameOptionsValue = "DENY";
            public const string XContentTypeOptionsValue = "nosniff";
            public const string XXssProtectionValue = "1; mode=block";
            public const string ContentSecurityPolicy = "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; font-src 'self' data:;";
            public const string ReferrerPolicyValue = "strict-origin-when-cross-origin";
        }

        /// <summary>
        /// Validation constants for business rules and limits.
        /// </summary>
        public static class Validation
        {
            // Salary and budget limits
            public const decimal MaxRealisticSalary = 10_000_000;
            public const decimal MaxRealisticBudget = 1_000_000_000;

            // File upload
            public const int UniqueFileNameGuidLength = 8;
        }

        /// <summary>
        /// Sensitive data field names for encryption and masking.
        /// </summary>
        public static class SensitiveFields
        {
            public const string NationalId = "NationalId";
            public const string PassportNumber = "PassportNumber";
            public const string BankAccount = "BankAccount";
            public const string Salary = "Salary";
            public const string BankName = "BankName";
            public const string BankBranch = "BankBranch";
        }

        /// <summary>
        /// Logging and structured-logging constants.
        /// </summary>
        public static class Logging
        {
            /// <summary>HTTP header name used to carry a correlation / trace identifier.</summary>
            public const string CorrelationIdHeader = "X-Correlation-ID";

            /// <summary>Key under which the correlation ID is stored in <c>HttpContext.Items</c>.</summary>
            public const string CorrelationIdItemKey = "CorrelationId";

            /// <summary>Property name used to enrich log entries with the correlation ID.</summary>
            public const string CorrelationIdProperty = "CorrelationId";

            /// <summary>Threshold (milliseconds) above which a request is considered slow.</summary>
            public const int SlowRequestThresholdMs = 5000;
        }

        /// <summary>
        /// Payroll calculation constants.
        /// </summary>
        public static class Payroll
        {
            // Tax brackets (annual income thresholds)
            public const decimal TaxBracket1Limit = 50_000m;
            public const decimal TaxBracket2Limit = 100_000m;
            public const decimal TaxBracket3Limit = 200_000m;

            // Tax rates
            public const decimal TaxRate1 = 0.10m;   // 10% up to bracket 1
            public const decimal TaxRate2 = 0.20m;   // 20% up to bracket 2
            public const decimal TaxRate3 = 0.30m;   // 30% up to bracket 3
            public const decimal TaxRate4 = 0.35m;   // 35% above bracket 3

            // Statutory deductions
            public const decimal ProvidentFundRate = 0.12m;  // 12% of basic salary
            public const decimal EmployerPfRate = 0.12m;     // 12% employer contribution
            public const decimal EsiRate = 0.0075m;          // 0.75% ESI (employee)
            public const decimal EmployerEsiRate = 0.0325m;  // 3.25% ESI (employer)
            public const decimal EsiEligibilityLimit = 21_000m; // Monthly gross limit for ESI

            // Allowances as percentage of basic salary
            public const decimal HraRate = 0.40m;            // HRA: 40% of basic
            public const decimal HraMetroRate = 0.50m;       // HRA metro: 50% of basic
            public const decimal ConveyanceAllowance = 1_600m; // Fixed monthly
            public const decimal MedicalAllowance = 1_250m;  // Fixed monthly

            // Standard deductions
            public const decimal StandardDeductionAnnual = 50_000m;

            // Months per year
            public const int MonthsPerYear = 12;

            // Cache keys
            public static string PayrollKey(int id) => $"payroll_{id}";
            public static string EmployeePayrollKey(int employeeId, int year, int month) =>
                $"payroll_emp_{employeeId}_{year}_{month}";
        }

        /// <summary>
        /// Performance review constants.
        /// </summary>
        public static class Performance
        {
            public const int MinRating = 1;
            public const int MaxRating = 5;
            public const double TargetAchievementWeight = 0.60;
            public const double CompetencyWeight = 0.40;
            public const int DefaultGoalCount = 5;
            public const int ReviewCycleDays = 365;

            // Cache keys
            public static string ReviewKey(int id) => $"review_{id}";
            public static string EmployeeReviewsKey(int employeeId) => $"employee_reviews_{employeeId}";
        }

        /// <summary>
        /// Attendance service constants.
        /// </summary>
        public static class Attendance
        {
            public const int StandardWorkHours = 8;
            public const int OvertimeThresholdMinutes = 30;
            public const int LateThresholdMinutes = 15;
            public const string DefaultWorkStartTime = "09:00";
        }

        /// <summary>
        /// Report constants for export and scheduling.
        /// </summary>
        public static class Reports
        {
            public const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            public const string PdfContentType = "application/pdf";
            public const string ExcelExtension = ".xlsx";
            public const string PdfExtension = ".pdf";
            public const int MaxExportRows = 10_000;

            // Worksheet names
            public const string EmployeeSheetName = "Employees";
            public const string LeaveSheetName = "Leave Requests";
            public const string AttendanceSheetName = "Attendance";
            public const string PayrollSheetName = "Payroll";
        }

        /// <summary>
        /// Health-check endpoint and check-name constants.
        /// </summary>
        public static class HealthChecks
        {
            /// <summary>Route for the detailed health-check endpoint.</summary>
            public const string DetailedEndpoint = "/health";

            /// <summary>Route for the lightweight liveness probe endpoint.</summary>
            public const string LiveEndpoint = "/health/live";

            /// <summary>Name of the database connectivity health check.</summary>
            public const string DatabaseCheckName = "database";

            /// <summary>Tag applied to infrastructure-level health checks.</summary>
            public const string InfrastructureTag = "infrastructure";

            /// <summary>Tag applied to business-level health checks.</summary>
            public const string BusinessTag = "business";
        }
    }
}
