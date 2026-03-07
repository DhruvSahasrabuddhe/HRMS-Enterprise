using HRMS.Core.Entities;
using HRMS.Core.Enums;

namespace HRMS.Core.Interfaces.Repositories
{
    /// <summary>
    /// Repository interface for payroll data access.
    /// </summary>
    public interface IPayrollRepository : IGenericRepository<Payroll>
    {
        /// <summary>Gets the payroll record for a specific employee, year and month.</summary>
        Task<Payroll?> GetPayrollByEmployeeAndPeriodAsync(int employeeId, int year, int month);

        /// <summary>Gets all payroll records for an employee.</summary>
        Task<IEnumerable<Payroll>> GetPayrollsByEmployeeAsync(int employeeId);

        /// <summary>Gets all payroll records for a specific month/year.</summary>
        Task<IEnumerable<Payroll>> GetPayrollsByPeriodAsync(int year, int month);

        /// <summary>Gets payroll records by status.</summary>
        Task<IEnumerable<Payroll>> GetPayrollsByStatusAsync(PayrollStatus status, int year, int month);

        /// <summary>Gets a payroll record with its navigation properties loaded.</summary>
        Task<Payroll?> GetPayrollWithDetailsAsync(int payrollId);

        /// <summary>Checks whether a payroll record already exists for an employee in a given period.</summary>
        Task<bool> PayrollExistsAsync(int employeeId, int year, int month);

        /// <summary>Gets total gross salary for a department in a given period.</summary>
        Task<decimal> GetTotalGrossForDepartmentAsync(int departmentId, int year, int month);

        /// <summary>Gets total net salary paid in a given period.</summary>
        Task<decimal> GetTotalNetPaidAsync(int year, int month);
    }
}
