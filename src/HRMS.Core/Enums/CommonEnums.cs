namespace HRMS.Core.Enums
{
    public enum Gender
    {
        Male = 1,
        Female = 2,
        Other = 3
    }

    public enum MaritalStatus
    {
        Single = 1,
        Married = 2,
        Divorced = 3,
        Widowed = 4
    }

    public enum EmploymentType
    {
        Permanent = 1,
        Contract = 2,
        Intern = 3,
        Probation = 4,
        Temporary = 5
    }

    public enum EmployeeStatus
    {
        Active = 1,
        Inactive = 2,
        OnLeave = 3,
        Terminated = 4,
        Resigned = 5
    }

    public enum LeaveType
    {
        Annual = 1,
        Sick = 2,
        Maternity = 3,
        Paternity = 4,
        Unpaid = 5,
        Compensatory = 6,
        Bereavement = 7,
        Marriage = 8
    }

    public enum LeaveStatus
    {
        Pending = 1,
        Approved = 2,
        Rejected = 3,
        Cancelled = 4,
        InProgress = 5
    }

    public enum AttendanceStatus
    {
        Present = 1,
        Absent = 2,
        Late = 3,
        HalfDay = 4,
        Holiday = 5,
        Weekend = 6,
        OnLeave = 7
    }

    public enum NotificationType
    {
        Info = 1,
        Success = 2,
        Warning = 3,
        Error = 4
    }

    public enum Permission
    {
        ViewEmployees = 1,
        CreateEmployee = 2,
        EditEmployee = 3,
        DeleteEmployee = 4,
        ViewDepartments = 5,
        CreateDepartment = 6,
        EditDepartment = 7,
        DeleteDepartment = 8,
        ManageLeaves = 9,
        ApproveLeaves = 10,
        ViewReports = 11,
        ManageUsers = 12,
        ManageRoles = 13,
        ManagePayroll = 14,
        ManagePerformance = 15
    }

    public enum PerformanceRating
    {
        Unsatisfactory = 1,
        NeedsImprovement = 2,
        MeetsExpectations = 3,
        ExceedsExpectations = 4,
        Outstanding = 5
    }

    public enum PerformanceReviewStatus
    {
        Draft = 1,
        SelfAssessmentPending = 2,
        ManagerReviewPending = 3,
        HrReviewPending = 4,
        Completed = 5,
        Acknowledged = 6
    }

    public enum ReviewCycleType
    {
        Annual = 1,
        MidYear = 2,
        Quarterly = 3,
        Probation = 4,
        Adhoc = 5
    }

    public enum PayrollStatus
    {
        Draft = 1,
        Processed = 2,
        Approved = 3,
        Paid = 4,
        Cancelled = 5
    }

    public enum PayFrequency
    {
        Monthly = 1,
        BiMonthly = 2,
        Weekly = 3
    }
}
