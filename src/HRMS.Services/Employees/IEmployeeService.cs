using HRMS.Services.Employees.Dtos;

namespace HRMS.Services.Employees
{
    /// <summary>
    /// Service interface for managing employee operations including CRUD, search, and import/export.
    /// </summary>
    public interface IEmployeeService
    {
        /// <summary>
        /// Retrieves an employee by their unique identifier.
        /// </summary>
        /// <param name="id">The employee ID.</param>
        /// <returns>The employee details or null if not found.</returns>
        Task<EmployeeDto?> GetEmployeeByIdAsync(int id);

        /// <summary>
        /// Retrieves all active employees in the system.
        /// </summary>
        /// <returns>A collection of employee list items.</returns>
        Task<IEnumerable<EmployeeListDto>> GetAllEmployeesAsync();

        /// <summary>
        /// Searches for employees based on the provided search criteria.
        /// </summary>
        /// <param name="searchDto">The search parameters including filters, sorting, and pagination.</param>
        /// <returns>A collection of matching employees.</returns>
        Task<IEnumerable<EmployeeListDto>> SearchEmployeesAsync(EmployeeSearchDto searchDto);

        /// <summary>
        /// Creates a new employee record.
        /// </summary>
        /// <param name="createDto">The employee data to create.</param>
        /// <returns>The created employee details.</returns>
        Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeDto createDto);

        /// <summary>
        /// Updates an existing employee record.
        /// </summary>
        /// <param name="updateDto">The employee data to update.</param>
        /// <returns>The updated employee details.</returns>
        Task<EmployeeDto> UpdateEmployeeAsync(UpdateEmployeeDto updateDto);

        /// <summary>
        /// Soft deletes an employee by marking them as deleted.
        /// </summary>
        /// <param name="id">The employee ID to delete.</param>
        /// <returns>True if deletion was successful, false otherwise.</returns>
        Task<bool> DeleteEmployeeAsync(int id);

        /// <summary>
        /// Checks if an email address is unique in the system.
        /// </summary>
        /// <param name="email">The email address to check.</param>
        /// <param name="excludeId">Optional employee ID to exclude from the check (for updates).</param>
        /// <returns>True if the email is unique, false otherwise.</returns>
        Task<bool> IsEmailUniqueAsync(string email, int? excludeId = null);

        /// <summary>
        /// Checks if an employee code is unique in the system.
        /// </summary>
        /// <param name="code">The employee code to check.</param>
        /// <param name="excludeId">Optional employee ID to exclude from the check (for updates).</param>
        /// <returns>True if the code is unique, false otherwise.</returns>
        Task<bool> IsEmployeeCodeUniqueAsync(string code, int? excludeId = null);

        /// <summary>
        /// Gets the total count of active employees.
        /// </summary>
        /// <returns>The number of active employees.</returns>
        Task<int> GetEmployeeCountAsync();

        /// <summary>
        /// Exports employees matching the search criteria to an Excel file.
        /// </summary>
        /// <param name="searchDto">The search criteria for filtering employees to export.</param>
        /// <returns>The Excel file as a byte array.</returns>
        Task<byte[]> ExportEmployeesToExcelAsync(EmployeeSearchDto searchDto);

        /// <summary>
        /// Imports employees from an Excel file.
        /// </summary>
        /// <param name="fileData">The Excel file data as a byte array.</param>
        /// <returns>The number of employees imported.</returns>
        Task<int> ImportEmployeesFromExcelAsync(byte[] fileData);

        /// <summary>
        /// Retrieves all employees in a specific department.
        /// </summary>
        /// <param name="departmentId">The department ID.</param>
        /// <returns>A collection of employees in the department.</returns>
        Task<IEnumerable<EmployeeListDto>> GetEmployeesByDepartmentAsync(int departmentId);

        /// <summary>
        /// Retrieves all employees reporting to a specific manager.
        /// </summary>
        /// <param name="managerId">The manager's employee ID.</param>
        /// <returns>A collection of employees reporting to the manager.</returns>
        Task<IEnumerable<EmployeeListDto>> GetEmployeesByManagerAsync(int managerId);
    }
}