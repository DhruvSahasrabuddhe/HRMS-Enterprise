using HRMS.Services.Departments;
using HRMS.Services.Departments.Dtos;
using HRMS.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Web.Controllers.Api.V1
{
    /// <summary>
    /// RESTful API for managing departments.
    /// </summary>
    /// <remarks>
    /// Department reference data (list endpoint) is suitable for short-lived public caching
    /// and therefore returns <c>Cache-Control: public, max-age=300</c> on GET collection
    /// requests.  Individual records and mutations always return <c>no-store</c>.
    /// </remarks>
    [Authorize(Roles = "Admin,HR")]
    public class DepartmentsController : ApiControllerBase
    {
        private readonly IDepartmentService _departmentService;
        private readonly ILogger<DepartmentsController> _logger;

        /// <summary>Initializes the controller with its required services.</summary>
        public DepartmentsController(IDepartmentService departmentService, ILogger<DepartmentsController> logger)
        {
            _departmentService = departmentService;
            _logger = logger;
        }

        // ── GET /api/v1/departments ───────────────────────────────────────────────

        /// <summary>Returns a list of all active departments.</summary>
        /// <response code="200">List of departments.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Insufficient role.</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<DepartmentListDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetDepartments()
        {
            AddRateLimitHeaders();
            // Department list is reference data – allow a short public cache.
            SetPublicCache(HrmsConstants.Cache.DefaultExpirationMinutes * 60);

            var departments = await _departmentService.GetAllDepartmentsAsync();
            return Ok(departments);
        }

        // ── GET /api/v1/departments/{id} ──────────────────────────────────────────

        /// <summary>Retrieves a single department by identifier.</summary>
        /// <param name="id">The unique department identifier.</param>
        /// <response code="200">Department details.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Insufficient role.</response>
        /// <response code="404">Department not found.</response>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(DepartmentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDepartment(int id)
        {
            AddRateLimitHeaders();
            SetNoCache();

            var department = await _departmentService.GetDepartmentByIdAsync(id);
            if (department == null)
            {
                return NotFound(new { errorCode = "DEPARTMENT_NOT_FOUND", message = $"Department with id {id} was not found." });
            }

            return Ok(department);
        }

        // ── GET /api/v1/departments/{id}/employees ────────────────────────────────

        /// <summary>Returns a department together with its employee roster.</summary>
        /// <param name="id">The unique department identifier.</param>
        /// <response code="200">Department detail with employees.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Insufficient role.</response>
        /// <response code="404">Department not found.</response>
        [HttpGet("{id:int}/employees")]
        [ProducesResponseType(typeof(DepartmentDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDepartmentWithEmployees(int id)
        {
            AddRateLimitHeaders();
            SetNoCache();

            var department = await _departmentService.GetDepartmentWithEmployeesAsync(id);
            if (department == null)
            {
                return NotFound(new { errorCode = "DEPARTMENT_NOT_FOUND", message = $"Department with id {id} was not found." });
            }

            return Ok(department);
        }

        // ── POST /api/v1/departments ──────────────────────────────────────────────

        /// <summary>Creates a new department.</summary>
        /// <param name="createDto">Department data.</param>
        /// <response code="201">Department created; Location header points to the new resource.</response>
        /// <response code="400">Validation failed.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Insufficient role.</response>
        [HttpPost]
        [ProducesResponseType(typeof(DepartmentDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateDepartment([FromBody] CreateDepartmentDto createDto)
        {
            AddRateLimitHeaders();

            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var department = await _departmentService.CreateDepartmentAsync(createDto);
            _logger.LogInformation("Department {DepartmentId} created via API", department.Id);

            return CreatedAtAction(nameof(GetDepartment), new { id = department.Id }, department);
        }

        // ── PUT /api/v1/departments/{id} ──────────────────────────────────────────

        /// <summary>Updates an existing department.</summary>
        /// <param name="id">The identifier of the department to update.</param>
        /// <param name="updateDto">Updated department data.</param>
        /// <response code="200">Department updated successfully.</response>
        /// <response code="400">Validation failed or id mismatch.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Insufficient role.</response>
        /// <response code="404">Department not found.</response>
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(DepartmentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateDepartment(int id, [FromBody] UpdateDepartmentDto updateDto)
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

            var department = await _departmentService.UpdateDepartmentAsync(updateDto);
            return Ok(department);
        }

        // ── DELETE /api/v1/departments/{id} ───────────────────────────────────────

        /// <summary>Deletes a department. Fails if the department still has employees.</summary>
        /// <param name="id">The identifier of the department to delete.</param>
        /// <response code="204">Deleted successfully.</response>
        /// <response code="400">Department still has employees assigned.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Admin role required.</response>
        /// <response code="404">Department not found.</response>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteDepartment(int id)
        {
            AddRateLimitHeaders();

            var deleted = await _departmentService.DeleteDepartmentAsync(id);
            if (!deleted)
            {
                return BadRequest(new
                {
                    errorCode = "DEPARTMENT_HAS_EMPLOYEES",
                    message = "Cannot delete a department that still has employees assigned to it."
                });
            }

            _logger.LogInformation("Department {DepartmentId} deleted via API", id);
            return NoContent();
        }

        // ── GET /api/v1/departments/employee-counts ───────────────────────────────

        /// <summary>Returns the employee count grouped by department.</summary>
        /// <response code="200">Dictionary of department name → employee count.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Insufficient role.</response>
        [HttpGet("employee-counts")]
        [ProducesResponseType(typeof(Dictionary<string, int>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetEmployeeCounts()
        {
            AddRateLimitHeaders();
            SetPublicCache(HrmsConstants.Cache.DefaultExpirationMinutes * 60);

            var counts = await _departmentService.GetEmployeeCountByDepartmentAsync();
            return Ok(counts);
        }
    }
}
