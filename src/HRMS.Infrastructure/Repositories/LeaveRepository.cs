using HRMS.Core.Entities;
using HRMS.Core.Enums;
using HRMS.Core.Interfaces.Repositories;
using HRMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Infrastructure.Repositories
{
    public class LeaveRepository : GenericRepository<LeaveRequest>, ILeaveRepository
    {
        public LeaveRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<LeaveRequest>> GetLeaveRequestsByEmployeeAsync(int employeeId)
        {
            return await _dbSet
                .Where(l => l.EmployeeId == employeeId)
                .Include(l => l.Employee)
                .Include(l => l.ApprovedBy)
                .OrderByDescending(l => l.StartDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<LeaveRequest>> GetLeaveRequestsByStatusAsync(LeaveStatus status)
        {
            return await _dbSet
                .Where(l => l.Status == status)
                .Include(l => l.Employee)
                .Include(l => l.ApprovedBy)
                .OrderByDescending(l => l.StartDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<LeaveRequest>> GetLeaveRequestsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _dbSet
                .Where(l => l.StartDate >= startDate && l.EndDate <= endDate)
                .Include(l => l.Employee)
                .Include(l => l.ApprovedBy)
                .OrderBy(l => l.StartDate)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalLeaveDaysAsync(int employeeId, int year, LeaveType? leaveType = null)
        {
            var query = _dbSet
                .Where(l => l.EmployeeId == employeeId
                    && l.StartDate.Year == year
                    && l.Status == LeaveStatus.Approved);

            if (leaveType.HasValue)
            {
                query = query.Where(l => l.LeaveType == leaveType.Value);
            }

            return await query.SumAsync(l => l.TotalDays);
        }

        public async Task<bool> HasOverlappingLeaveAsync(int employeeId, DateTime startDate, DateTime endDate, int? excludeId = null)
        {
            var query = _dbSet
                .Where(l => l.EmployeeId == employeeId
                    && l.Status != LeaveStatus.Cancelled
                    && l.Status != LeaveStatus.Rejected
                    && ((l.StartDate <= endDate && l.EndDate >= startDate))); // Overlapping condition

            if (excludeId.HasValue)
            {
                query = query.Where(l => l.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task<IEnumerable<LeaveRequest>> GetPendingApprovalsAsync(int managerId)
        {
            // Single JOIN query – avoids the two-trip N+1 pattern.
            return await _dbSet
                .Where(l => l.Status == LeaveStatus.Pending
                         && l.Employee != null
                         && l.Employee.ManagerId == managerId)
                .Include(l => l.Employee)
                .Include(l => l.ApprovedBy)
                .OrderBy(l => l.StartDate)
                .ToListAsync();
        }

        public async Task<Dictionary<LeaveType, decimal>> GetLeaveBalanceAsync(int employeeId, int year)
        {
            var approvedLeaves = await _dbSet
                .Where(l => l.EmployeeId == employeeId
                    && l.StartDate.Year == year
                    && l.Status == LeaveStatus.Approved)
                .GroupBy(l => l.LeaveType)
                .Select(g => new { LeaveType = g.Key, TotalDays = g.Sum(l => l.TotalDays) })
                .ToDictionaryAsync(g => g.LeaveType, g => g.TotalDays);

            return approvedLeaves;
        }

        public async Task<IEnumerable<LeaveRequest>> GetLeaveRequestsByDepartmentAsync(int departmentId, DateTime startDate, DateTime endDate)
        {
            // Single JOIN query – avoids the two-trip N+1 pattern.
            return await _dbSet
                .Where(l => l.Employee != null
                         && l.Employee.DepartmentId == departmentId
                         && l.StartDate >= startDate
                         && l.EndDate <= endDate)
                .Include(l => l.Employee)
                .Include(l => l.ApprovedBy)
                .OrderBy(l => l.StartDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<LeaveRequest>> GetUpcomingLeavesAsync(int days)
        {
            var startDate = DateTime.Today;
            var endDate = startDate.AddDays(days);

            return await _dbSet
                .Where(l => l.StartDate >= startDate
                    && l.StartDate <= endDate
                    && l.Status == LeaveStatus.Approved)
                .Include(l => l.Employee)
                .Include(l => l.Employee.Department)
                .OrderBy(l => l.StartDate)
                .ToListAsync();
        }

        public async Task<int> GetEmployeesOnLeaveAsync(DateTime date)
        {
            return await _dbSet
                .Where(l => l.StartDate <= date
                    && l.EndDate >= date
                    && l.Status == LeaveStatus.Approved)
                .Select(l => l.EmployeeId)
                .Distinct()
                .CountAsync();
        }

        public async Task<Dictionary<LeaveStatus, int>> GetLeaveStatisticsAsync(DateTime startDate, DateTime endDate)
        {
            var statistics = await _dbSet
                .Where(l => l.StartDate >= startDate && l.EndDate <= endDate)
                .GroupBy(l => l.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.Status, g => g.Count);

            return statistics;
        }

        public async Task<bool> CanApplyLeaveAsync(int employeeId, LeaveType leaveType, DateTime startDate, DateTime endDate)
        {
            var employee = await _context.Set<Employee>()
                .FirstOrDefaultAsync(e => e.Id == employeeId);

            if (employee == null)
                return false;

            // Check probation period
            if (employee.ProbationEndDate.HasValue && employee.ProbationEndDate > DateTime.Today)
            {
                // Employees on probation might have restrictions
                if (leaveType != LeaveType.Sick)
                    return false;
            }

            // Check if already on leave
            var hasOverlap = await HasOverlappingLeaveAsync(employeeId, startDate, endDate);
            if (hasOverlap)
                return false;

            // Check minimum notice period (e.g., 2 days for casual leave)
            if (leaveType != LeaveType.Sick && startDate < DateTime.Today.AddDays(2))
                return false;

            return true;
        }

        public async Task<int> GetAvailableLeaveDaysAsync(int employeeId, int year, LeaveType leaveType)
        {
            var maxAllowedDays = GetMaxLeaveDaysByType(leaveType);
            var usedDays = await GetTotalLeaveDaysAsync(employeeId, year, leaveType);

            return (int)(maxAllowedDays - usedDays);
        }

        private decimal GetMaxLeaveDaysByType(LeaveType leaveType)
        {
            return leaveType switch
            {
                LeaveType.Annual => 20,
                LeaveType.Sick => 12,
                LeaveType.Maternity => 180,
                LeaveType.Paternity => 15,
                LeaveType.Unpaid => 365,
                LeaveType.Compensatory => 30,
                LeaveType.Bereavement => 5,
                LeaveType.Marriage => 5,
                _ => 0
            };
        }
    }
}