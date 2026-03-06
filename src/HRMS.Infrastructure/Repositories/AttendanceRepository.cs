using HRMS.Core.Entities;
using HRMS.Core.Enums;
using HRMS.Core.Interfaces.Repositories;
using HRMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Infrastructure.Repositories
{
    public class AttendanceRepository : GenericRepository<Attendance>, IAttendanceRepository
    {
        public AttendanceRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Attendance?> GetAttendanceByEmployeeAndDateAsync(int employeeId, DateTime date)
        {
            return await _dbSet
                .Include(a => a.Employee)
                .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.Date.Date == date.Date);
        }

        public async Task<IEnumerable<Attendance>> GetAttendanceByEmployeeAsync(
            int employeeId, DateTime startDate, DateTime endDate)
        {
            return await _dbSet
                .Where(a => a.EmployeeId == employeeId
                         && a.Date.Date >= startDate.Date
                         && a.Date.Date <= endDate.Date)
                .Include(a => a.Employee)
                .OrderBy(a => a.Date)
                .ToListAsync();
        }

        public async Task<IEnumerable<Attendance>> GetAttendanceByDateAsync(DateTime date)
        {
            return await _dbSet
                .Where(a => a.Date.Date == date.Date)
                .Include(a => a.Employee)
                .OrderBy(a => a.Employee.LastName)
                .ToListAsync();
        }

        public async Task<IEnumerable<Attendance>> GetAttendanceByStatusAsync(
            int employeeId, AttendanceStatus status, int year, int month)
        {
            return await _dbSet
                .Where(a => a.EmployeeId == employeeId
                         && a.Status == status
                         && a.Date.Year == year
                         && a.Date.Month == month)
                .OrderBy(a => a.Date)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalOvertimeHoursAsync(
            int employeeId, DateTime startDate, DateTime endDate)
        {
            return await _dbSet
                .Where(a => a.EmployeeId == employeeId
                         && a.Date.Date >= startDate.Date
                         && a.Date.Date <= endDate.Date)
                .SumAsync(a => a.OvertimeHours);
        }

        public async Task<int> GetAbsentDaysAsync(
            int employeeId, DateTime startDate, DateTime endDate)
        {
            return await _dbSet
                .CountAsync(a => a.EmployeeId == employeeId
                              && a.Date.Date >= startDate.Date
                              && a.Date.Date <= endDate.Date
                              && a.Status == AttendanceStatus.Absent);
        }

        public async Task<double> GetAttendancePercentageAsync(
            int employeeId, DateTime startDate, DateTime endDate)
        {
            var total = await _dbSet
                .CountAsync(a => a.EmployeeId == employeeId
                              && a.Date.Date >= startDate.Date
                              && a.Date.Date <= endDate.Date
                              && a.Status != AttendanceStatus.Holiday
                              && a.Status != AttendanceStatus.Weekend);

            if (total == 0) return 0;

            var present = await _dbSet
                .CountAsync(a => a.EmployeeId == employeeId
                              && a.Date.Date >= startDate.Date
                              && a.Date.Date <= endDate.Date
                              && (a.Status == AttendanceStatus.Present
                                  || a.Status == AttendanceStatus.Late
                                  || a.Status == AttendanceStatus.HalfDay));

            return Math.Round((double)present / total * 100, 2);
        }

        public async Task<bool> HasCheckedInTodayAsync(int employeeId, DateTime date)
        {
            return await _dbSet
                .AnyAsync(a => a.EmployeeId == employeeId
                            && a.Date.Date == date.Date
                            && a.CheckInTime.HasValue);
        }
    }
}
