using HRMS.Core.Enums;
using HRMS.Core.Interfaces.Repositories;
using HRMS.Services.Reports.Dtos;
using Microsoft.Extensions.Logging;
using System.Text;

namespace HRMS.Services.Reports
{
    /// <summary>
    /// Service for generating HRMS reports and exporting data to Excel (CSV-based XLSX).
    /// </summary>
    public class ReportService : IReportService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ReportService> _logger;

        public ReportService(IUnitOfWork unitOfWork, ILogger<ReportService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<EmployeeReportDto> GetEmployeeReportAsync()
        {
            try
            {
                _logger.LogInformation("Generating employee report");

                var employees = (await _unitOfWork.Employees.GetAllAsync()).ToList();

                var report = new EmployeeReportDto
                {
                    ReportDate = DateTime.UtcNow,
                    TotalEmployees = employees.Count,
                    ActiveEmployees = employees.Count(e => e.Status == EmployeeStatus.Active),
                    InactiveEmployees = employees.Count(e => e.Status == EmployeeStatus.Inactive),
                    TerminatedEmployees = employees.Count(e =>
                        e.Status == EmployeeStatus.Terminated || e.Status == EmployeeStatus.Resigned),
                    EmployeesByDepartment = employees
                        .GroupBy(e => e.Department?.Name ?? "Unknown")
                        .ToDictionary(g => g.Key, g => g.Count()),
                    EmployeesByType = employees
                        .GroupBy(e => e.EmploymentType)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    EmployeesByGender = employees
                        .GroupBy(e => e.Gender)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    NewHires = employees
                        .Where(e => e.HireDate >= DateTime.UtcNow.AddMonths(-1))
                        .Select(e => new NewHireDto
                        {
                            EmployeeName = e.FullName,
                            JobTitle = e.JobTitle,
                            Department = e.Department?.Name ?? string.Empty,
                            HireDate = e.HireDate
                        })
                        .OrderByDescending(n => n.HireDate)
                        .ToList()
                };

                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating employee report");
                throw;
            }
        }

        public async Task<LeaveReportDto> GetLeaveReportAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                _logger.LogInformation("Generating leave report for {Start} to {End}", startDate, endDate);

                var leaves = (await _unitOfWork.Leaves.GetLeaveRequestsByDateRangeAsync(startDate, endDate)).ToList();

                var report = new LeaveReportDto
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    TotalRequests = leaves.Count,
                    ApprovedRequests = leaves.Count(l => l.Status == LeaveStatus.Approved),
                    PendingRequests = leaves.Count(l => l.Status == LeaveStatus.Pending),
                    RejectedRequests = leaves.Count(l => l.Status == LeaveStatus.Rejected),
                    TotalLeaveDays = leaves.Where(l => l.Status == LeaveStatus.Approved).Sum(l => l.TotalDays),
                    LeaveSummary = leaves
                        .GroupBy(l => l.LeaveType)
                        .ToDictionary(
                            g => g.Key,
                            g => new LeaveSummaryDto
                            {
                                RequestCount = g.Count(),
                                TotalDays = g.Where(l => l.Status == LeaveStatus.Approved).Sum(l => l.TotalDays),
                                EmployeesCount = g.Select(l => l.EmployeeId).Distinct().Count()
                            }),
                    TopLeaveTakers = leaves
                        .Where(l => l.Status == LeaveStatus.Approved)
                        .GroupBy(l => l.EmployeeId)
                        .Select(g => new EmployeeLeaveDto
                        {
                            EmployeeName = g.First().Employee?.FullName ?? string.Empty,
                            Department = g.First().Employee?.Department?.Name ?? string.Empty,
                            TotalDays = g.Sum(l => l.TotalDays),
                            RequestCount = g.Count()
                        })
                        .OrderByDescending(e => e.TotalDays)
                        .Take(10)
                        .ToList()
                };

                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating leave report");
                throw;
            }
        }

        public async Task<AttendanceReportDto> GetAttendanceReportAsync(int year, int month)
        {
            try
            {
                _logger.LogInformation("Generating attendance report for {Year}/{Month}", year, month);

                var startDate = new DateTime(year, month, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);

                // Working days = days excluding weekends
                int workingDays = 0;
                for (var d = startDate; d <= endDate; d = d.AddDays(1))
                    if (d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday)
                        workingDays++;

                var employees = (await _unitOfWork.Employees.GetAllAsync())
                    .Where(e => e.Status == EmployeeStatus.Active)
                    .ToList();

                var employeeAttendance = new Dictionary<string, AttendanceSummaryDto>();
                int totalAbsences = 0, totalLates = 0;
                double totalPercentage = 0;

                foreach (var emp in employees)
                {
                    var records = (await _unitOfWork.Attendances.GetAttendanceByEmployeeAsync(
                        emp.Id, startDate, endDate)).ToList();

                    var present = records.Count(r =>
                        r.Status == AttendanceStatus.Present || r.Status == AttendanceStatus.Late || r.Status == AttendanceStatus.HalfDay);
                    var absent = records.Count(r => r.Status == AttendanceStatus.Absent);
                    var late = records.Count(r => r.Status == AttendanceStatus.Late);
                    var percentage = workingDays > 0 ? Math.Round((double)present / workingDays * 100, 2) : 0;

                    employeeAttendance[emp.FullName] = new AttendanceSummaryDto
                    {
                        Present = present,
                        Absent = absent,
                        Late = late,
                        HalfDay = records.Count(r => r.Status == AttendanceStatus.HalfDay),
                        OnLeave = records.Count(r => r.Status == AttendanceStatus.OnLeave),
                        AttendancePercentage = percentage
                    };

                    totalAbsences += absent;
                    totalLates += late;
                    totalPercentage += percentage;
                }

                var report = new AttendanceReportDto
                {
                    Month = startDate,
                    TotalWorkingDays = workingDays,
                    AverageAttendance = employees.Count > 0
                        ? Math.Round(totalPercentage / employees.Count, 2) : 0,
                    TotalAbsences = totalAbsences,
                    TotalLates = totalLates,
                    EmployeeAttendance = employeeAttendance
                };

                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating attendance report");
                throw;
            }
        }

        public async Task<byte[]> ExportEmployeesToExcelAsync()
        {
            try
            {
                _logger.LogInformation("Exporting employees to Excel");

                var employees = (await _unitOfWork.Employees.GetAllAsync()).ToList();

                var sb = new StringBuilder();
                sb.AppendLine("Employee Code,First Name,Last Name,Email,Department,Job Title,Employment Type,Status,Hire Date,Salary");

                foreach (var e in employees)
                {
                    sb.AppendLine(string.Join(",",
                        CsvEscape(e.EmployeeCode),
                        CsvEscape(e.FirstName),
                        CsvEscape(e.LastName),
                        CsvEscape(e.Email),
                        CsvEscape(e.Department?.Name ?? string.Empty),
                        CsvEscape(e.JobTitle),
                        e.EmploymentType.ToString(),
                        e.Status.ToString(),
                        e.HireDate.ToString("yyyy-MM-dd"),
                        e.Salary.ToString("F2")));
                }

                return Encoding.UTF8.GetBytes(sb.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting employees to Excel");
                throw;
            }
        }

        public async Task<byte[]> ExportLeaveReportToExcelAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                _logger.LogInformation("Exporting leave report to Excel");

                var leaves = (await _unitOfWork.Leaves.GetLeaveRequestsByDateRangeAsync(startDate, endDate)).ToList();

                var sb = new StringBuilder();
                sb.AppendLine("Employee,Employee Code,Leave Type,Start Date,End Date,Total Days,Status,Reason,Approved By,Applied Date");

                foreach (var l in leaves)
                {
                    sb.AppendLine(string.Join(",",
                        CsvEscape(l.Employee?.FullName ?? string.Empty),
                        CsvEscape(l.Employee?.EmployeeCode ?? string.Empty),
                        l.LeaveType.ToString(),
                        l.StartDate.ToString("yyyy-MM-dd"),
                        l.EndDate.ToString("yyyy-MM-dd"),
                        l.TotalDays.ToString("F1"),
                        l.Status.ToString(),
                        CsvEscape(l.Reason ?? string.Empty),
                        CsvEscape(l.ApprovedBy?.FullName ?? string.Empty),
                        l.CreatedAt.ToString("yyyy-MM-dd")));
                }

                return Encoding.UTF8.GetBytes(sb.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting leave report to Excel");
                throw;
            }
        }

        public async Task<byte[]> ExportAttendanceReportToExcelAsync(int year, int month)
        {
            try
            {
                _logger.LogInformation("Exporting attendance report to Excel for {Year}/{Month}", year, month);

                var report = await GetAttendanceReportAsync(year, month);

                var sb = new StringBuilder();
                sb.AppendLine($"Attendance Report - {new DateTime(year, month, 1):MMMM yyyy}");
                sb.AppendLine($"Working Days,{report.TotalWorkingDays}");
                sb.AppendLine($"Average Attendance %,{report.AverageAttendance:F1}");
                sb.AppendLine();
                sb.AppendLine("Employee Name,Present,Absent,Late,Half Day,On Leave,Attendance %");

                foreach (var (name, summary) in report.EmployeeAttendance)
                {
                    sb.AppendLine(string.Join(",",
                        CsvEscape(name),
                        summary.Present,
                        summary.Absent,
                        summary.Late,
                        summary.HalfDay,
                        summary.OnLeave,
                        summary.AttendancePercentage.ToString("F1")));
                }

                return Encoding.UTF8.GetBytes(sb.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting attendance report");
                throw;
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────────────

        private static string CsvEscape(string value)
        {
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
                return $"\"{value.Replace("\"", "\"\"")}\"";
            return value;
        }
    }
}
