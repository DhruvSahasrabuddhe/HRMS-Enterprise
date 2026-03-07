using HRMS.Core.Entities;
using HRMS.Core.Enums;
using HRMS.Core.Interfaces.Repositories;
using HRMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Infrastructure.Repositories
{
    public class PayrollRepository : GenericRepository<Payroll>, IPayrollRepository
    {
        public PayrollRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Payroll?> GetPayrollByEmployeeAndPeriodAsync(int employeeId, int year, int month)
        {
            return await _dbSet
                .Include(p => p.Employee)
                .FirstOrDefaultAsync(p => p.EmployeeId == employeeId && p.Year == year && p.Month == month);
        }

        public async Task<IEnumerable<Payroll>> GetPayrollsByEmployeeAsync(int employeeId)
        {
            return await _dbSet
                .Where(p => p.EmployeeId == employeeId)
                .Include(p => p.Employee)
                .OrderByDescending(p => p.Year)
                .ThenByDescending(p => p.Month)
                .ToListAsync();
        }

        public async Task<IEnumerable<Payroll>> GetPayrollsByPeriodAsync(int year, int month)
        {
            return await _dbSet
                .Where(p => p.Year == year && p.Month == month)
                .Include(p => p.Employee)
                    .ThenInclude(e => e.Department)
                .OrderBy(p => p.Employee.LastName)
                .ToListAsync();
        }

        public async Task<IEnumerable<Payroll>> GetPayrollsByStatusAsync(PayrollStatus status, int year, int month)
        {
            return await _dbSet
                .Where(p => p.Status == status && p.Year == year && p.Month == month)
                .Include(p => p.Employee)
                    .ThenInclude(e => e.Department)
                .OrderBy(p => p.Employee.LastName)
                .ToListAsync();
        }

        public async Task<Payroll?> GetPayrollWithDetailsAsync(int payrollId)
        {
            return await _dbSet
                .Include(p => p.Employee)
                    .ThenInclude(e => e.Department)
                .Include(p => p.ProcessedBy)
                .Include(p => p.ApprovedBy)
                .FirstOrDefaultAsync(p => p.Id == payrollId);
        }

        public async Task<bool> PayrollExistsAsync(int employeeId, int year, int month)
        {
            return await _dbSet
                .AnyAsync(p => p.EmployeeId == employeeId && p.Year == year && p.Month == month);
        }

        public async Task<decimal> GetTotalGrossForDepartmentAsync(int departmentId, int year, int month)
        {
            return await _dbSet
                .Where(p => p.Employee.DepartmentId == departmentId
                         && p.Year == year
                         && p.Month == month
                         && p.Status != PayrollStatus.Cancelled)
                .SumAsync(p => p.GrossSalary);
        }

        public async Task<decimal> GetTotalNetPaidAsync(int year, int month)
        {
            return await _dbSet
                .Where(p => p.Year == year
                         && p.Month == month
                         && (p.Status == PayrollStatus.Paid || p.Status == PayrollStatus.Approved))
                .SumAsync(p => p.NetSalary);
        }
    }
}
