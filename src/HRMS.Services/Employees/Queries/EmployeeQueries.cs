using HRMS.Core.CQRS;
using HRMS.Core.Enums;
using HRMS.Services.Employees.Dtos;

namespace HRMS.Services.Employees.Queries
{
    /// <summary>Query to retrieve a single employee by ID with full details.</summary>
    public sealed class GetEmployeeByIdQuery : IQuery<EmployeeDto?>
    {
        public int EmployeeId { get; }

        public GetEmployeeByIdQuery(int employeeId)
        {
            EmployeeId = employeeId;
        }
    }

    /// <summary>Query to retrieve all employees (lightweight list view).</summary>
    public sealed class GetAllEmployeesQuery : IQuery<IEnumerable<EmployeeListDto>>
    {
    }

    /// <summary>Query to search employees with optional filters and pagination.</summary>
    public sealed class SearchEmployeesQuery : IQuery<IEnumerable<EmployeeListDto>>
    {
        public string? SearchTerm { get; init; }
        public int? DepartmentId { get; init; }
        public int? ManagerId { get; init; }
        public EmployeeStatus? Status { get; init; }
        public string? SortBy { get; init; }
        public bool SortDescending { get; init; }
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 10;
    }
}
