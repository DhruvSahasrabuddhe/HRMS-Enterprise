using HRMS.Core.Enums;
using HRMS.Services.Attendance.Dtos;

namespace HRMS.Services.Attendance
{
    /// <summary>
    /// Service interface for managing employee attendance records.
    /// </summary>
    public interface IAttendanceService
    {
        /// <summary>Gets an attendance record by ID.</summary>
        Task<AttendanceDto?> GetAttendanceByIdAsync(int id);

        /// <summary>Gets attendance records for an employee within a date range.</summary>
        Task<IEnumerable<AttendanceDto>> GetAttendanceByEmployeeAsync(int employeeId, DateTime startDate, DateTime endDate);

        /// <summary>Gets all attendance records for a specific date.</summary>
        Task<IEnumerable<AttendanceDto>> GetAttendanceByDateAsync(DateTime date);

        /// <summary>Gets attendance records filtered by status for a given month.</summary>
        Task<IEnumerable<AttendanceDto>> GetAttendanceByStatusAsync(int employeeId, AttendanceStatus status, int year, int month);

        /// <summary>Creates a new attendance record.</summary>
        Task<AttendanceDto> CreateAttendanceAsync(CreateAttendanceDto createDto);

        /// <summary>Updates an existing attendance record.</summary>
        Task<AttendanceDto> UpdateAttendanceAsync(UpdateAttendanceDto updateDto);

        /// <summary>Records an employee check-in.</summary>
        Task<AttendanceDto> CheckInAsync(CheckInDto checkInDto);

        /// <summary>Records an employee check-out.</summary>
        Task<AttendanceDto> CheckOutAsync(CheckOutDto checkOutDto);

        /// <summary>Deletes an attendance record.</summary>
        Task<bool> DeleteAttendanceAsync(int id);

        /// <summary>Gets a monthly attendance summary for an employee.</summary>
        Task<AttendanceSummaryDto> GetMonthlySummaryAsync(int employeeId, int year, int month);

        /// <summary>Gets the total overtime hours for an employee within a date range.</summary>
        Task<decimal> GetTotalOvertimeHoursAsync(int employeeId, DateTime startDate, DateTime endDate);

        /// <summary>Gets the attendance percentage for an employee within a date range.</summary>
        Task<double> GetAttendancePercentageAsync(int employeeId, DateTime startDate, DateTime endDate);

        /// <summary>Checks whether an employee has already checked in today.</summary>
        Task<bool> HasCheckedInTodayAsync(int employeeId);
    }
}
