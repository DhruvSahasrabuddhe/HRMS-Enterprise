using HRMS.Core.Enums;
using HRMS.Services.Employees;
using HRMS.Services.Employees.Dtos;
using HRMS.Shared.Common;
using HRMS.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Web.Controllers.Api.V1
{
    /// <summary>
    /// RESTful API for managing employees.
    /// All endpoints require authentication; administrative mutations require the Admin or HR role.
    /// </summary>
    /// <remarks>
    /// **Pagination** – collection responses include <c>X-Total-Count</c>, <c>X-Total-Pages</c>,
    /// <c>X-Current-Page</c>, and <c>X-Page-Size</c> response headers.
    ///
    /// **Rate limiting** – every response carries <c>X-RateLimit-Limit</c>,
    /// <c>X-RateLimit-Remaining</c>, and <c>X-RateLimit-Reset</c> headers.
    ///
    /// **Errors** – all error responses follow the RFC 7807 Problem Details format
    /// with an additional <c>errorCode</c> and <c>correlationId</c> field.
    /// </remarks>
    [Authorize(Roles = "Admin,HR")]
    public class EmployeesController : ApiControllerBase
    {
        private readonly IEmployeeService _employeeService;
        private readonly ILogger<EmployeesController> _logger;

        /// <summary>Initializes the controller with its required services.</summary>
        public EmployeesController(IEmployeeService employeeService, ILogger<EmployeesController> logger)
        {
            _employeeService = employeeService;
            _logger = logger;
        }

        // ── GET /api/v1/employees ─────────────────────────────────────────────────

        /// <summary>Searches and lists employees with optional filtering, sorting, and pagination.</summary>
        /// <param name="searchTerm">Optional full-text search across name, email, and employee code.</param>
        /// <param name="departmentId">Filter by department identifier.</param>
        /// <param name="status">Filter by employee status (Active, Inactive, OnLeave, Terminated, Resigned).</param>
        /// <param name="managerId">Filter by reporting manager identifier.</param>
        /// <param name="pageNumber">1-based page number (default: 1).</param>
        /// <param name="pageSize">Number of records per page (default: 10, max: 100).</param>
        /// <param name="sortBy">Field to sort by (default: LastName).</param>
        /// <param name="sortAscending">Sort direction: true = ascending, false = descending (default: true).</param>
        /// <response code="200">Returns the list of matching employees with pagination headers.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Insufficient role to access this endpoint.</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<EmployeeListDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetEmployees(
            [FromQuery] string? searchTerm,
            [FromQuery] int? departmentId,
            [FromQuery] EmployeeStatus? status,
            [FromQuery] int? managerId,
            [FromQuery] int pageNumber = HrmsConstants.Pagination.DefaultPageNumber,
            [FromQuery] int pageSize = HrmsConstants.Pagination.DefaultPageSize,
            [FromQuery] string sortBy = "LastName",
            [FromQuery] bool sortAscending = true)
        {
            AddRateLimitHeaders();
            SetNoCache();

            pageSize = Math.Min(pageSize, HrmsConstants.Pagination.MaxPageSize);

            var searchDto = new EmployeeSearchDto
            {
                SearchTerm = searchTerm,
                DepartmentId = departmentId,
                Status = status,
                ManagerId = managerId,
                PageNumber = pageNumber,
                PageSize = pageSize,
                SortBy = sortBy,
                SortAscending = sortAscending
            };

            var employees = await _employeeService.SearchEmployeesAsync(searchDto);
            var total = await _employeeService.GetEmployeeCountAsync();

            var paged = new PagedResult<EmployeeListDto>(
                employees.ToList().AsReadOnly(), total, pageNumber, pageSize);

            AddPaginationHeaders(paged);
            return Ok(paged.Items);
        }

        // ── GET /api/v1/employees/{id} ────────────────────────────────────────────

        /// <summary>Retrieves a single employee by their identifier.</summary>
        /// <param name="id">The unique employee identifier.</param>
        /// <response code="200">Returns the employee details.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Insufficient role.</response>
        /// <response code="404">Employee not found.</response>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(EmployeeDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEmployee(int id)
        {
            AddRateLimitHeaders();
            SetNoCache();

            var employee = await _employeeService.GetEmployeeByIdAsync(id);
            if (employee == null)
            {
                return NotFound(new { errorCode = "EMPLOYEE_NOT_FOUND", message = $"Employee with id {id} was not found." });
            }

            return Ok(employee);
        }

        // ── POST /api/v1/employees ────────────────────────────────────────────────

        /// <summary>Creates a new employee record.</summary>
        /// <param name="createDto">The employee data to create.</param>
        /// <response code="201">Employee created successfully; Location header points to the new resource.</response>
        /// <response code="400">Validation failed.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Insufficient role.</response>
        /// <response code="422">Business rule violation (e.g. duplicate email).</response>
        [HttpPost]
        [ProducesResponseType(typeof(EmployeeDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeDto createDto)
        {
            AddRateLimitHeaders();

            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var employee = await _employeeService.CreateEmployeeAsync(createDto);
            _logger.LogInformation("Employee {EmployeeId} created via API", employee.Id);

            return CreatedAtAction(nameof(GetEmployee), new { id = employee.Id }, employee);
        }

        // ── PUT /api/v1/employees/{id} ────────────────────────────────────────────

        /// <summary>Updates an existing employee record.</summary>
        /// <param name="id">The identifier of the employee to update.</param>
        /// <param name="updateDto">The updated employee data.</param>
        /// <response code="200">Employee updated successfully.</response>
        /// <response code="400">Validation failed or id mismatch.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Insufficient role.</response>
        /// <response code="404">Employee not found.</response>
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(EmployeeDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateEmployee(int id, [FromBody] UpdateEmployeeDto updateDto)
        {
            AddRateLimitHeaders();

            if (id != updateDto.Id)
            {
                return BadRequest(new { errorCode = "ID_MISMATCH", message = "Route id must match body id." });
            }

            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var employee = await _employeeService.UpdateEmployeeAsync(updateDto);
            return Ok(employee);
        }

        // ── DELETE /api/v1/employees/{id} ─────────────────────────────────────────

        /// <summary>Soft-deletes an employee by marking them as deleted.</summary>
        /// <param name="id">The identifier of the employee to delete.</param>
        /// <response code="204">Employee deleted successfully; no content.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Insufficient role (Admin only).</response>
        /// <response code="404">Employee not found.</response>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            AddRateLimitHeaders();

            var deleted = await _employeeService.DeleteEmployeeAsync(id);
            if (!deleted)
            {
                return NotFound(new { errorCode = "EMPLOYEE_NOT_FOUND", message = $"Employee with id {id} was not found." });
            }

            _logger.LogInformation("Employee {EmployeeId} deleted via API", id);
            return NoContent();
        }

        // ── GET /api/v1/employees/{id}/department-members ─────────────────────────

        /// <summary>Returns all employees in the same department as the specified employee.</summary>
        /// <param name="departmentId">The department identifier.</param>
        /// <response code="200">List of employees in the department.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Insufficient role.</response>
        [HttpGet("by-department/{departmentId:int}")]
        [ProducesResponseType(typeof(IEnumerable<EmployeeListDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetEmployeesByDepartment(int departmentId)
        {
            AddRateLimitHeaders();
            SetNoCache();

            var employees = await _employeeService.GetEmployeesByDepartmentAsync(departmentId);
            return Ok(employees);
        }
    }
}
