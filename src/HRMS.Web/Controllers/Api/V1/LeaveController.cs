using HRMS.Core.Enums;
using HRMS.Services.Leave;
using HRMS.Services.Leave.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Web.Controllers.Api.V1
{
    /// <summary>
    /// RESTful API for managing employee leave requests, approvals, and balances.
    /// </summary>
    /// <remarks>
    /// Workflow summary:
    /// <list type="number">
    ///   <item>Employee submits leave via <c>POST /api/v1/leave</c>.</item>
    ///   <item>Manager or HR approves via <c>POST /api/v1/leave/{id}/approve</c>
    ///         or rejects via <c>POST /api/v1/leave/{id}/reject</c>.</item>
    ///   <item>Employee may cancel a pending request via <c>POST /api/v1/leave/{id}/cancel</c>.</item>
    /// </list>
    /// </remarks>
    [Authorize(Roles = "Admin,HR,Manager")]
    public class LeaveController : ApiControllerBase
    {
        private readonly ILeaveService _leaveService;
        private readonly ILogger<LeaveController> _logger;

        /// <summary>Initializes the controller with its required services.</summary>
        public LeaveController(ILeaveService leaveService, ILogger<LeaveController> logger)
        {
            _leaveService = leaveService;
            _logger = logger;
        }

        // ── GET /api/v1/leave ─────────────────────────────────────────────────────

        /// <summary>Returns all leave requests in the system.</summary>
        /// <response code="200">List of leave requests.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Insufficient role.</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<LeaveRequestDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetLeaveRequests()
        {
            AddRateLimitHeaders();
            SetNoCache();

            var leaves = await _leaveService.GetAllLeaveRequestsAsync();
            return Ok(leaves);
        }

        // ── GET /api/v1/leave/{id} ────────────────────────────────────────────────

        /// <summary>Returns a single leave request by identifier.</summary>
        /// <param name="id">The leave request identifier.</param>
        /// <response code="200">Leave request details.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Insufficient role.</response>
        /// <response code="404">Leave request not found.</response>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(LeaveRequestDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetLeaveRequest(int id)
        {
            AddRateLimitHeaders();
            SetNoCache();

            var leave = await _leaveService.GetLeaveRequestByIdAsync(id);
            if (leave == null)
            {
                return NotFound(new { errorCode = "LEAVE_NOT_FOUND", message = $"Leave request with id {id} was not found." });
            }

            return Ok(leave);
        }

        // ── GET /api/v1/leave/employee/{employeeId} ───────────────────────────────

        /// <summary>Returns all leave requests for a specific employee.</summary>
        /// <param name="employeeId">The employee identifier.</param>
        /// <response code="200">Leave requests for the employee.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Insufficient role.</response>
        [HttpGet("employee/{employeeId:int}")]
        [ProducesResponseType(typeof(IEnumerable<LeaveRequestDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetLeaveRequestsByEmployee(int employeeId)
        {
            AddRateLimitHeaders();
            SetNoCache();

            var leaves = await _leaveService.GetLeaveRequestsByEmployeeAsync(employeeId);
            return Ok(leaves);
        }

        // ── GET /api/v1/leave/employee/{employeeId}/balance ───────────────────────

        /// <summary>Returns the leave balance for an employee for a given year.</summary>
        /// <param name="employeeId">The employee identifier.</param>
        /// <param name="year">The calendar year (defaults to current year).</param>
        /// <response code="200">Leave balance per leave type.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Insufficient role.</response>
        [HttpGet("employee/{employeeId:int}/balance")]
        [ProducesResponseType(typeof(LeaveBalanceDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetLeaveBalance(int employeeId, [FromQuery] int? year)
        {
            AddRateLimitHeaders();
            SetNoCache();

            var balance = await _leaveService.GetLeaveBalanceAsync(employeeId, year ?? DateTime.UtcNow.Year);
            return Ok(balance);
        }

        // ── GET /api/v1/leave/pending/{managerId} ─────────────────────────────────

        /// <summary>Returns leave requests pending approval for a manager's direct reports.</summary>
        /// <param name="managerId">The manager's employee identifier.</param>
        /// <response code="200">Pending leave requests for the manager's team.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Insufficient role.</response>
        [HttpGet("pending/{managerId:int}")]
        [ProducesResponseType(typeof(IEnumerable<LeaveRequestDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetPendingApprovals(int managerId)
        {
            AddRateLimitHeaders();
            SetNoCache();

            var leaves = await _leaveService.GetPendingApprovalsAsync(managerId);
            return Ok(leaves);
        }

        // ── POST /api/v1/leave ────────────────────────────────────────────────────

        /// <summary>Submits a new leave request on behalf of an employee.</summary>
        /// <param name="createDto">Leave request data.</param>
        /// <response code="201">Leave request created; Location header points to the new resource.</response>
        /// <response code="400">Validation failed.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Insufficient role.</response>
        /// <response code="422">Business rule violation (e.g. insufficient balance, overlapping dates).</response>
        [HttpPost]
        [ProducesResponseType(typeof(LeaveRequestDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> CreateLeaveRequest([FromBody] CreateLeaveRequestDto createDto)
        {
            AddRateLimitHeaders();

            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var leave = await _leaveService.CreateLeaveRequestAsync(createDto);
            _logger.LogInformation("Leave request {LeaveId} created via API for employee {EmployeeId}",
                leave.Id, leave.EmployeeId);

            return CreatedAtAction(nameof(GetLeaveRequest), new { id = leave.Id }, leave);
        }

        // ── PUT /api/v1/leave/{id} ────────────────────────────────────────────────

        /// <summary>Updates a pending leave request.</summary>
        /// <param name="id">The leave request identifier.</param>
        /// <param name="updateDto">Updated leave data.</param>
        /// <response code="200">Leave request updated.</response>
        /// <response code="400">Validation failed or id mismatch.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Insufficient role.</response>
        /// <response code="404">Leave request not found.</response>
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(LeaveRequestDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateLeaveRequest(int id, [FromBody] UpdateLeaveRequestDto updateDto)
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

            var leave = await _leaveService.UpdateLeaveRequestAsync(updateDto);
            return Ok(leave);
        }

        // ── POST /api/v1/leave/{id}/approve ──────────────────────────────────────

        /// <summary>Approves a pending leave request.</summary>
        /// <param name="id">The leave request identifier.</param>
        /// <param name="approveDto">Approval data (optional remarks).</param>
        /// <response code="200">Leave request approved.</response>
        /// <response code="400">Leave request is not in a pending state.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Manager or Admin/HR role required.</response>
        /// <response code="404">Leave request not found.</response>
        [HttpPost("{id:int}/approve")]
        [ProducesResponseType(typeof(LeaveRequestDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ApproveLeaveRequest(int id, [FromBody] ApproveLeaveDto approveDto)
        {
            AddRateLimitHeaders();

            if (id != approveDto.Id)
            {
                return BadRequest(new { errorCode = "ID_MISMATCH", message = "Route id must match body id." });
            }

            // TODO: resolve approverId from the authenticated user's claims in a real implementation.
            var leave = await _leaveService.ApproveLeaveRequestAsync(approveDto, approverId: 0);
            _logger.LogInformation("Leave request {LeaveId} approved via API", leave.Id);

            return Ok(leave);
        }

        // ── POST /api/v1/leave/{id}/reject ────────────────────────────────────────

        /// <summary>Rejects a pending leave request.</summary>
        /// <param name="id">The leave request identifier.</param>
        /// <param name="rejectDto">Rejection data including reason.</param>
        /// <response code="200">Leave request rejected.</response>
        /// <response code="400">Leave request is not in a pending state.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Manager or Admin/HR role required.</response>
        /// <response code="404">Leave request not found.</response>
        [HttpPost("{id:int}/reject")]
        [ProducesResponseType(typeof(LeaveRequestDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RejectLeaveRequest(int id, [FromBody] RejectLeaveDto rejectDto)
        {
            AddRateLimitHeaders();

            if (id != rejectDto.Id)
            {
                return BadRequest(new { errorCode = "ID_MISMATCH", message = "Route id must match body id." });
            }

            var leave = await _leaveService.RejectLeaveRequestAsync(rejectDto, approverId: 0);
            _logger.LogInformation("Leave request {LeaveId} rejected via API", leave.Id);

            return Ok(leave);
        }

        // ── POST /api/v1/leave/{id}/cancel ────────────────────────────────────────

        /// <summary>Cancels a pending leave request (employee action).</summary>
        /// <param name="id">The leave request identifier.</param>
        /// <param name="employeeId">The identifier of the employee cancelling the request.</param>
        /// <response code="204">Leave request cancelled successfully.</response>
        /// <response code="400">Leave request cannot be cancelled (e.g. already approved).</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Insufficient role.</response>
        /// <response code="404">Leave request not found.</response>
        [HttpPost("{id:int}/cancel")]
        [Authorize(Roles = "Admin,HR,Manager,Employee")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CancelLeaveRequest(int id, [FromQuery] int employeeId)
        {
            AddRateLimitHeaders();

            var cancelled = await _leaveService.CancelLeaveRequestAsync(id, employeeId);
            if (!cancelled)
            {
                return BadRequest(new
                {
                    errorCode = "LEAVE_CANCEL_FAILED",
                    message = "Leave request could not be cancelled. It may already be approved or not belong to the employee."
                });
            }

            return NoContent();
        }

        // ── DELETE /api/v1/leave/{id} ─────────────────────────────────────────────

        /// <summary>Permanently deletes a leave request (Admin only).</summary>
        /// <param name="id">The leave request identifier.</param>
        /// <response code="204">Deleted successfully.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Admin role required.</response>
        /// <response code="404">Leave request not found.</response>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteLeaveRequest(int id)
        {
            AddRateLimitHeaders();

            var deleted = await _leaveService.DeleteLeaveRequestAsync(id);
            if (!deleted)
            {
                return NotFound(new { errorCode = "LEAVE_NOT_FOUND", message = $"Leave request with id {id} was not found." });
            }

            _logger.LogInformation("Leave request {LeaveId} deleted via API", id);
            return NoContent();
        }
    }
}
