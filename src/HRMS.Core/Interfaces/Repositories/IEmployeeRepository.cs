using HRMS.Core.Entities;
using HRMS.Core.Enums;

namespace HRMS.Core.Interfaces.Repositories
{
    public interface IEmployeeRepository : IGenericRepository<Employee>
    {
        Task<Employee?> GetEmployeeWithDetailsAsync(int id);
        Task<IEnumerable<Employee>> GetEmployeesByDepartmentAsync(int departmentId);
        Task<IEnumerable<Employee>> GetEmployeesByManagerAsync(int managerId);
        Task<IEnumerable<Employee>> SearchEmployeesAsync(string searchTerm);
        Task<bool> IsEmailUniqueAsync(string email, int? excludeId = null);
        Task<bool> IsEmployeeCodeUniqueAsync(string code, int? excludeId = null);
        Task<int> GetEmployeeCountByDepartmentAsync(int departmentId);
        Task<IEnumerable<Employee>> GetEmployeesHiredBetweenAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<Employee>> GetEmployeesWithUpcomingBirthdaysAsync(int days);

        /// <summary>
        /// Returns a filtered, sorted and paginated employee list entirely within the database –
        /// avoids loading the full result set into memory before applying ordering and pagination.
        /// </summary>
        /// <param name="searchTerm">Optional free-text filter applied to name, e-mail and code.</param>
        /// <param name="departmentId">Optional department filter.</param>
        /// <param name="managerId">Optional manager filter.</param>
        /// <param name="status">Optional status filter.</param>
        /// <param name="sortBy">Column to sort by (e.g. "FirstName", "LastName", "HireDate", "Department").</param>
        /// <param name="sortAscending">Sort direction.</param>
        /// <param name="pageNumber">1-based page number.</param>
        /// <param name="pageSize">Number of records per page.</param>
        Task<IEnumerable<Employee>> SearchEmployeesPagedAsync(
            string? searchTerm,
            int? departmentId,
            int? managerId,
            EmployeeStatus? status,
            string sortBy,
            bool sortAscending,
            int pageNumber,
            int pageSize);
    }
}
