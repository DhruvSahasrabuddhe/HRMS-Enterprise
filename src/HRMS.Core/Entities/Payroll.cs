using HRMS.Core.Entities.Base;
using HRMS.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace HRMS.Core.Entities
{
    public class Payroll : BaseEntity
    {
        public int EmployeeId { get; set; }

        public int Year { get; set; }

        public int Month { get; set; }

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

        // Net pay
        public decimal NetSalary { get; set; }

        // Employer contributions (informational)
        public decimal EmployerProvidentFund { get; set; }

        public decimal EmployerEsi { get; set; }

        // Attendance-based adjustments
        public int WorkingDays { get; set; }

        public int PaidDays { get; set; }

        public int LopDays { get; set; }          // Loss of pay days

        public decimal LopDeduction { get; set; }

        // Leave encashment
        public decimal LeaveEncashment { get; set; }

        // Payment details
        public DateTime? PaymentDate { get; set; }

        [StringLength(100)]
        public string? PaymentReference { get; set; }

        [StringLength(500)]
        public string? Remarks { get; set; }

        // Processing info
        public int? ProcessedById { get; set; }

        public DateTime? ProcessedDate { get; set; }

        public int? ApprovedById { get; set; }

        public DateTime? ApprovedDate { get; set; }

        // Navigation properties
        public virtual Employee Employee { get; set; } = null!;
        public virtual Employee? ProcessedBy { get; set; }
        public virtual Employee? ApprovedBy { get; set; }
    }
}
