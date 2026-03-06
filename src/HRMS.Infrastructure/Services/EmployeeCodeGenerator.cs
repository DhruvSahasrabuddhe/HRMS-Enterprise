using HRMS.Core.Interfaces.Repositories;
using HRMS.Core.Interfaces.Services;
using HRMS.Shared.Constants;

namespace HRMS.Infrastructure.Services
{
    /// <summary>
    /// Default implementation of employee code generator.
    /// Generates codes in format: EMP{YYYY}{SEQUENCE} (e.g., EMP202600001).
    /// </summary>
    public class EmployeeCodeGenerator : IEmployeeCodeGenerator
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDateTimeProvider _dateTimeProvider;

        public EmployeeCodeGenerator(IUnitOfWork unitOfWork, IDateTimeProvider dateTimeProvider)
        {
            _unitOfWork = unitOfWork;
            _dateTimeProvider = dateTimeProvider;
        }

        /// <inheritdoc />
        public async Task<string> GenerateEmployeeCodeAsync()
        {
            var currentYear = _dateTimeProvider.Today.Year;
            var yearString = currentYear.ToString();

            // Get all employees with codes starting with the prefix for current year
            var allEmployees = await _unitOfWork.Employees.GetAllAsync();
            var currentYearEmployees = allEmployees
                .Where(e => e.EmployeeCode.StartsWith($"{HrmsConstants.Employee.CodePrefix}{yearString}"))
                .ToList();

            int nextNumber = 1;
            if (currentYearEmployees.Any())
            {
                // Extract the numeric part from existing codes
                var maxNumber = currentYearEmployees
                    .Select(e => e.EmployeeCode.Substring(
                        HrmsConstants.Employee.CodePrefix.Length + HrmsConstants.Employee.CodeYearLength))
                    .Where(code => int.TryParse(code, out _))
                    .Select(int.Parse)
                    .DefaultIfEmpty(0)
                    .Max();

                nextNumber = maxNumber + 1;
            }

            // Format: EMP{YYYY}{SEQUENCE} - e.g., EMP202600001
            return $"{HrmsConstants.Employee.CodePrefix}{yearString}{nextNumber:D5}";
        }
    }
}
