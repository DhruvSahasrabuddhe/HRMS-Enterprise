using HRMS.Core.Enums;

namespace HRMS.Services.Attendance.Dtos
{
    public class AttendanceDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string EmployeeCode { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public DateTime? CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public TimeSpan? TotalHours { get; set; }
        public AttendanceStatus Status { get; set; }
        public decimal OvertimeHours { get; set; }
        public decimal LateMinutes { get; set; }
        public string? Notes { get; set; }
    }

    public class CreateAttendanceDto
    {
        public int EmployeeId { get; set; }
        public DateTime Date { get; set; }
        public DateTime? CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public AttendanceStatus Status { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateAttendanceDto
    {
        public int Id { get; set; }
        public DateTime? CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public AttendanceStatus Status { get; set; }
        public string? Notes { get; set; }
    }

    public class CheckInDto
    {
        public int EmployeeId { get; set; }
        public DateTime CheckInTime { get; set; }
        public string? Notes { get; set; }
    }

    public class CheckOutDto
    {
        public int EmployeeId { get; set; }
        public DateTime CheckOutTime { get; set; }
        public string? Notes { get; set; }
    }

    public class AttendanceSummaryDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public int Year { get; set; }
        public int Month { get; set; }
        public int TotalDays { get; set; }
        public int PresentDays { get; set; }
        public int AbsentDays { get; set; }
        public int LateDays { get; set; }
        public int HalfDays { get; set; }
        public int OnLeaveDays { get; set; }
        public decimal TotalOvertimeHours { get; set; }
        public double AttendancePercentage { get; set; }
    }
}
