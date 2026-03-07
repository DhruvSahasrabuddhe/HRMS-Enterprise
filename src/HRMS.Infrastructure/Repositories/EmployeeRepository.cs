using HRMS.Core.Entities;
using HRMS.Core.Enums;
using HRMS.Core.Interfaces.Repositories;
using HRMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Infrastructure.Repositories
{
    public class EmployeeRepository : GenericRepository<Employee>, IEmployeeRepository
    {
        public EmployeeRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Employee?> GetEmployeeWithDetailsAsync(int id)
        {
            return await _dbSet
                .Include(e => e.Department)
                .Include(e => e.Manager)
                .Include(e => e.Subordinates)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<IEnumerable<Employee>> GetEmployeesByDepartmentAsync(int departmentId)
        {
            return await _dbSet
                .Where(e => e.DepartmentId == departmentId)
                .Include(e => e.Department)
                .ToListAsync();
        }

        public async Task<IEnumerable<Employee>> GetEmployeesByManagerAsync(int managerId)
        {
            return await _dbSet
                .Where(e => e.ManagerId == managerId)
                .Include(e => e.Department)
                .ToListAsync();
        }

        public async Task<IEnumerable<Employee>> SearchEmployeesAsync(string searchTerm)
        {
            searchTerm = searchTerm.ToLower();
            return await _dbSet
                .Where(e => e.FirstName.ToLower().Contains(searchTerm) ||
                           e.LastName.ToLower().Contains(searchTerm) ||
                           e.Email.ToLower().Contains(searchTerm) ||
                           e.EmployeeCode.ToLower().Contains(searchTerm))
                .Include(e => e.Department)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Employee>> SearchEmployeesPagedAsync(
            string? searchTerm,
            int? departmentId,
            int? managerId,
            EmployeeStatus? status,
            string sortBy,
            bool sortAscending,
            int pageNumber,
            int pageSize)
        {
            var query = _dbSet.Include(e => e.Department).AsQueryable();

            // ── Filtering ──────────────────────────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.ToLower();
                query = query.Where(e =>
                    e.FirstName.ToLower().Contains(term) ||
                    e.LastName.ToLower().Contains(term) ||
                    e.Email.ToLower().Contains(term) ||
                    e.EmployeeCode.ToLower().Contains(term));
            }

            if (departmentId.HasValue)
                query = query.Where(e => e.DepartmentId == departmentId.Value);

            if (managerId.HasValue)
                query = query.Where(e => e.ManagerId == managerId.Value);

            if (status.HasValue)
                query = query.Where(e => e.Status == status.Value);

            // ── Sorting (database-side) ────────────────────────────────────────────
            query = sortBy.ToLower() switch
            {
                "firstname" => sortAscending
                    ? query.OrderBy(e => e.FirstName)
                    : query.OrderByDescending(e => e.FirstName),
                "hiredate" => sortAscending
                    ? query.OrderBy(e => e.HireDate)
                    : query.OrderByDescending(e => e.HireDate),
                "department" => sortAscending
                    ? query.OrderBy(e => e.Department != null ? e.Department.Name : string.Empty)
                    : query.OrderByDescending(e => e.Department != null ? e.Department.Name : string.Empty),
                _ => sortAscending   // default: sort by LastName
                    ? query.OrderBy(e => e.LastName)
                    : query.OrderByDescending(e => e.LastName)
            };

            // ── Pagination (database-side) ─────────────────────────────────────────
            return await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<bool> IsEmailUniqueAsync(string email, int? excludeId = null)
        {
            if (excludeId.HasValue)
            {
                return !await _dbSet.AnyAsync(e => e.Email == email && e.Id != excludeId.Value);
            }
            return !await _dbSet.AnyAsync(e => e.Email == email);
        }

        public async Task<bool> IsEmployeeCodeUniqueAsync(string code, int? excludeId = null)
        {
            if (excludeId.HasValue)
            {
                return !await _dbSet.AnyAsync(e => e.EmployeeCode == code && e.Id != excludeId.Value);
            }
            return !await _dbSet.AnyAsync(e => e.EmployeeCode == code);
        }

        public async Task<int> GetEmployeeCountByDepartmentAsync(int departmentId)
        {
            return await _dbSet.CountAsync(e => e.DepartmentId == departmentId);
        }

        public async Task<IEnumerable<Employee>> GetEmployeesHiredBetweenAsync(DateTime startDate, DateTime endDate)
        {
            return await _dbSet
                .Where(e => e.HireDate >= startDate && e.HireDate <= endDate)
                .Include(e => e.Department)
                .ToListAsync();
        }

        public async Task<IEnumerable<Employee>> GetEmployeesWithUpcomingBirthdaysAsync(int days)
        {
            var today = DateTime.Today;
            var targetDate = today.AddDays(days);

            return await _dbSet
                .Where(e => e.DateOfBirth.Month >= today.Month &&
                           e.DateOfBirth.DayOfYear <= targetDate.DayOfYear)
                .OrderBy(e => e.DateOfBirth)
                .ToListAsync();
        }
    }
}
