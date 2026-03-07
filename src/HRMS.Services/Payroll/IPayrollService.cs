using HRMS.Services.Payroll.Dtos;

namespace HRMS.Services.Payroll
{
    /// <summary>
    /// Service interface for payroll processing, salary calculation, and tax computation.
    /// </summary>
    public interface IPayrollService
    {
        /// <summary>Gets a payroll record by ID.</summary>
        Task<PayrollDto?> GetPayrollByIdAsync(int id);

        /// <summary>Gets the payroll record for an employee in a specific month/year.</summary>
        Task<PayrollDto?> GetPayrollByEmployeeAndPeriodAsync(int employeeId, int year, int month);

        /// <summary>Gets all payroll records for an employee.</summary>
        Task<IEnumerable<PayrollDto>> GetPayrollsByEmployeeAsync(int employeeId);

        /// <summary>Gets all payroll records for a given month/year.</summary>
        Task<IEnumerable<PayrollDto>> GetPayrollsByPeriodAsync(int year, int month);

        /// <summary>Processes payroll for a single employee for the given month.</summary>
        Task<PayrollDto> ProcessPayrollAsync(ProcessPayrollDto processDto, int processedById);

        /// <summary>Bulk-processes payroll for all active employees (optionally filtered by department).</summary>
        Task<int> BulkProcessPayrollAsync(BulkProcessPayrollDto bulkDto);

        /// <summary>Approves a processed payroll record.</summary>
        Task<PayrollDto> ApprovePayrollAsync(ApprovePayrollDto approveDto);

        /// <summary>Marks a payroll record as paid and records payment details.</summary>
        Task<PayrollDto> MarkAsPaidAsync(MarkAsPaidDto markAsPaidDto);

        /// <summary>Cancels a payroll record that has not yet been paid.</summary>
        Task<bool> CancelPayrollAsync(int payrollId, string? reason);

        /// <summary>Gets a summary of payroll totals for a given month/year.</summary>
        Task<PayrollSummaryDto> GetPayrollSummaryAsync(int year, int month);

        /// <summary>
        /// Returns a computed salary breakdown for an employee based on their
        /// current salary, showing all components and estimated deductions.
        /// </summary>
        Task<SalaryBreakdownDto> GetSalaryBreakdownAsync(int employeeId);

        /// <summary>
        /// Calculates the income tax for a given annual taxable income using
        /// the configured tax bracket rules.
        /// </summary>
        decimal CalculateIncomeTax(decimal annualTaxableIncome);
    }
}
