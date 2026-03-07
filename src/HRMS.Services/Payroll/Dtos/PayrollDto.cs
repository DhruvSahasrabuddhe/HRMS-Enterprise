using HRMS.Core.Enums;

namespace HRMS.Services.Payroll.Dtos
{
    public class PayrollDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string EmployeeCode { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public int Year { get; set; }
        public int Month { get; set; }
        public string PeriodLabel => $"{new DateTime(Year, Month, 1):MMMM yyyy}";
        public PayFrequency PayFrequency { get; set; }
        public PayrollStatus Status { get; set; }

        // Earnings
        public decimal BasicSalary { get; set; }
        public decimal HouseRentAllowance { get; set; }
        public decimal ConveyanceAllowance { get; set; }
        public decimal MedicalAllowance { get; set; }
        public decimal OtherAllowances { get; set; }
        public decimal OvertimePay { get; set; }
        public decimal GrossSalary { get; set; }

        // Deductions
        public decimal ProvidentFund { get; set; }
        public decimal EmployeeStateInsurance { get; set; }
        public decimal IncomeTax { get; set; }
        public decimal ProfessionalTax { get; set; }
        public decimal LoanDeduction { get; set; }
        public decimal OtherDeductions { get; set; }
        public decimal TotalDeductions { get; set; }

        // Net
        public decimal NetSalary { get; set; }

        // Employer
        public decimal EmployerProvidentFund { get; set; }
        public decimal EmployerEsi { get; set; }

        // Attendance
        public int WorkingDays { get; set; }
        public int PaidDays { get; set; }
        public int LopDays { get; set; }
        public decimal LopDeduction { get; set; }
        public decimal LeaveEncashment { get; set; }

        // Payment
        public DateTime? PaymentDate { get; set; }
        public string? PaymentReference { get; set; }
        public string? Remarks { get; set; }

        // Processing
        public string? ProcessedByName { get; set; }
        public DateTime? ProcessedDate { get; set; }
        public string? ApprovedByName { get; set; }
        public DateTime? ApprovedDate { get; set; }
    }

    public class ProcessPayrollDto
    {
        public int EmployeeId { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public int WorkingDays { get; set; }
        public int PaidDays { get; set; }
        public int LopDays { get; set; }
        public decimal OtherAllowances { get; set; }
        public decimal LoanDeduction { get; set; }
        public decimal OtherDeductions { get; set; }
        public decimal LeaveEncashment { get; set; }
        public string? Remarks { get; set; }
    }

    public class BulkProcessPayrollDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int? DepartmentId { get; set; }
        public int WorkingDays { get; set; } = 26;
        public int ProcessedById { get; set; }
    }

    public class ApprovePayrollDto
    {
        public int PayrollId { get; set; }
        public int ApprovedById { get; set; }
        public string? Remarks { get; set; }
    }

    public class MarkAsPaidDto
    {
        public int PayrollId { get; set; }
        public DateTime PaymentDate { get; set; }
        public string? PaymentReference { get; set; }
    }

    public class PayrollSummaryDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int TotalEmployees { get; set; }
        public decimal TotalGross { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal TotalNet { get; set; }
        public decimal TotalEmployerContributions { get; set; }
        public int ProcessedCount { get; set; }
        public int ApprovedCount { get; set; }
        public int PaidCount { get; set; }
        public int PendingCount { get; set; }
    }

    public class SalaryBreakdownDto
    {
        public decimal AnnualSalary { get; set; }
        public decimal MonthlySalary { get; set; }
        public decimal BasicSalary { get; set; }
        public decimal HouseRentAllowance { get; set; }
        public decimal ConveyanceAllowance { get; set; }
        public decimal MedicalAllowance { get; set; }
        public decimal OtherAllowances { get; set; }
        public decimal GrossSalary { get; set; }
        public decimal ProvidentFund { get; set; }
        public decimal EmployeeStateInsurance { get; set; }
        public decimal EstimatedIncomeTax { get; set; }
        public decimal ProfessionalTax { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal NetSalary { get; set; }
    }
}
