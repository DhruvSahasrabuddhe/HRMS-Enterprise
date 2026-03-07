using HRMS.Services.Reports.Dtos;

namespace HRMS.Services.Reports
{
    /// <summary>
    /// Service interface for generating HRMS reports and exporting data.
    /// </summary>
    public interface IReportService
    {
        /// <summary>Generates an employee report for the current date.</summary>
        Task<EmployeeReportDto> GetEmployeeReportAsync();

        /// <summary>Generates a leave report for the specified date range.</summary>
        Task<LeaveReportDto> GetLeaveReportAsync(DateTime startDate, DateTime endDate);

        /// <summary>Generates a monthly attendance report.</summary>
        Task<AttendanceReportDto> GetAttendanceReportAsync(int year, int month);

        /// <summary>Exports employee data to an Excel workbook.</summary>
        Task<byte[]> ExportEmployeesToExcelAsync();

        /// <summary>Exports leave report data to an Excel workbook.</summary>
        Task<byte[]> ExportLeaveReportToExcelAsync(DateTime startDate, DateTime endDate);

        /// <summary>Exports attendance data to an Excel workbook.</summary>
        Task<byte[]> ExportAttendanceReportToExcelAsync(int year, int month);
    }
}
