using HRMS.Services.Departments.Dtos;

namespace HRMS.Services.Departments
{
    /// <summary>
    /// Service interface for managing department operations including CRUD and statistics.
    /// </summary>
    public interface IDepartmentService
    {
        // Get operations
        /// <summary>
        /// Retrieves a department by its unique identifier.
        /// </summary>
        /// <param name="id">The department ID.</param>
        /// <returns>The department details or null if not found.</returns>
        Task<DepartmentDto?> GetDepartmentByIdAsync(int id);

        /// <summary>
        /// Retrieves all active departments in the system.
        /// </summary>
        /// <returns>A collection of department list items.</returns>
        Task<IEnumerable<DepartmentListDto>> GetAllDepartmentsAsync();

        /// <summary>
        /// Retrieves a department with its associated employees.
        /// </summary>
        /// <param name="id">The department ID.</param>
        /// <returns>The department details with employee information or null if not found.</returns>
        Task<DepartmentDetailDto?> GetDepartmentWithEmployeesAsync(int id);

        // Create/Update/Delete
        /// <summary>
        /// Creates a new department record.
        /// </summary>
        /// <param name="createDto">The department data to create.</param>
        /// <returns>The created department details.</returns>
        Task<DepartmentDto> CreateDepartmentAsync(CreateDepartmentDto createDto);

        /// <summary>
        /// Updates an existing department record.
        /// </summary>
        /// <param name="updateDto">The department data to update.</param>
        /// <returns>The updated department details.</returns>
        Task<DepartmentDto> UpdateDepartmentAsync(UpdateDepartmentDto updateDto);

        /// <summary>
        /// Soft deletes a department by marking it as deleted.
        /// </summary>
        /// <param name="id">The department ID to delete.</param>
        /// <returns>True if deletion was successful, false otherwise.</returns>
        Task<bool> DeleteDepartmentAsync(int id);

        // Validation
        /// <summary>
        /// Checks if a department code is unique in the system.
        /// </summary>
        /// <param name="code">The department code to check.</param>
        /// <param name="excludeId">Optional department ID to exclude from the check (for updates).</param>
        /// <returns>True if the code is unique, false otherwise.</returns>
        Task<bool> IsDepartmentCodeUniqueAsync(string code, int? excludeId = null);

        /// <summary>
        /// Checks if a department name is unique in the system.
        /// </summary>
        /// <param name="name">The department name to check.</param>
        /// <param name="excludeId">Optional department ID to exclude from the check (for updates).</param>
        /// <returns>True if the name is unique, false otherwise.</returns>
        Task<bool> IsDepartmentNameUniqueAsync(string name, int? excludeId = null);

        // Statistics
        /// <summary>
        /// Gets the total count of active departments.
        /// </summary>
        /// <returns>The number of active departments.</returns>
        Task<int> GetDepartmentCountAsync();

        /// <summary>
        /// Gets the employee count grouped by department.
        /// </summary>
        /// <returns>A dictionary with department names as keys and employee counts as values.</returns>
        Task<Dictionary<string, int>> GetEmployeeCountByDepartmentAsync();
    }
}