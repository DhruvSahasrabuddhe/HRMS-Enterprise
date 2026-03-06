using HRMS.Core.Entities;
using HRMS.Core.Enums;

namespace HRMS.Core.Interfaces.Repositories
{
    public interface IAttendanceRepository : IGenericRepository<Attendance>
    {
        Task<Attendance?> GetAttendanceByEmployeeAndDateAsync(int employeeId, DateTime date);
        Task<IEnumerable<Attendance>> GetAttendanceByEmployeeAsync(int employeeId, DateTime startDate, DateTime endDate);
        Task<IEnumerable<Attendance>> GetAttendanceByDateAsync(DateTime date);
        Task<IEnumerable<Attendance>> GetAttendanceByStatusAsync(int employeeId, AttendanceStatus status, int year, int month);
        Task<decimal> GetTotalOvertimeHoursAsync(int employeeId, DateTime startDate, DateTime endDate);
        Task<int> GetAbsentDaysAsync(int employeeId, DateTime startDate, DateTime endDate);
        Task<double> GetAttendancePercentageAsync(int employeeId, DateTime startDate, DateTime endDate);
        Task<bool> HasCheckedInTodayAsync(int employeeId, DateTime date);
    }
}
