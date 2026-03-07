using HRMS.Core.Enums;
using HRMS.Services.Attendance;
using HRMS.Services.Attendance.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Web.Controllers.Api.V1
{
    /// <summary>
    /// RESTful API for managing employee attendance records, check-ins, and check-outs.
    /// </summary>
    /// <remarks>
    /// Clock-in/clock-out workflow:
    /// <list type="number">
    ///   <item>Employee checks in: <c>POST /api/v1/attendance/check-in</c>.</item>
    ///   <item>Employee checks out: <c>POST /api/v1/attendance/check-out</c>.</item>
    /// </list>
    /// Monthly summaries are available at <c>GET /api/v1/attendance/employee/{id}/summary</c>.
    /// </remarks>
    [Authorize(Roles = "Admin,HR,Manager")]
    [IgnoreAntiforgeryToken]
    public class AttendanceController : ApiControllerBase
    {
        private readonly IAttendanceService _attendanceService;
        private readonly ILogger<AttendanceController> _logger;

        /// <summary>Initializes the controller with its required services.</summary>
        public AttendanceController(IAttendanceService attendanceService, ILogger<AttendanceController> logger)
        {
            _attendanceService = attendanceService;
            _logger = logger;
        }

        // ── GET /api/v1/attendance/{id} ───────────────────────────────────────────

        /// <summary>Returns a single attendance record by identifier.</summary>
        /// <param name="id">The attendance record identifier.</param>
        /// <response code="200">Attendance record details.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Insufficient role.</response>
        /// <response code="404">Attendance record not found.</response>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(AttendanceDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAttendance(int id)
        {
            AddRateLimitHeaders();
            SetNoCache();

            var attendance = await _attendanceService.GetAttendanceByIdAsync(id);
            if (attendance == null)
            {
                return NotFound(new { errorCode = "ATTENDANCE_NOT_FOUND", message = $"Attendance record with id {id} was not found." });
            }

            return Ok(attendance);
        }

        // ── GET /api/v1/attendance/employee/{employeeId} ──────────────────────────

        /// <summary>Returns attendance records for an employee within a date range.</summary>
        /// <param name="employeeId">The employee identifier.</param>
        /// <param name="startDate">Range start date (ISO 8601, e.g. 2025-01-01).</param>
        /// <param name="endDate">Range end date (ISO 8601, e.g. 2025-01-31).</param>
        /// <response code="200">Attendance records for the specified period.</response>
        /// <response code="400">Invalid date range.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Insufficient role.</response>
        [HttpGet("employee/{employeeId:int}")]
        [ProducesResponseType(typeof(IEnumerable<AttendanceDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAttendanceByEmployee(
            int employeeId,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            AddRateLimitHeaders();
            SetNoCache();

            if (endDate < startDate)
            {
                return BadRequest(new { errorCode = "INVALID_DATE_RANGE", message = "endDate must be on or after startDate." });
            }

            var records = await _attendanceService.GetAttendanceByEmployeeAsync(employeeId, startDate, endDate);
            return Ok(records);
        }

        // ── GET /api/v1/attendance/employee/{employeeId}/summary ─────────────────

        /// <summary>Returns a monthly attendance summary for an employee.</summary>
        /// <param name="employeeId">The employee identifier.</param>
        /// <param name="year">The year (e.g. 2025).</param>
        /// <param name="month">The month number (1–12).</param>
        /// <response code="200">Monthly summary including present, absent, late, and overtime counts.</response>
        /// <response code="400">Invalid year or month.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Insufficient role.</response>
        [HttpGet("employee/{employeeId:int}/summary")]
        [ProducesResponseType(typeof(AttendanceSummaryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetMonthlySummary(int employeeId, [FromQuery] int year, [FromQuery] int month)
        {
            AddRateLimitHeaders();
            SetNoCache();

            if (month < 1 || month > 12)
            {
                return BadRequest(new { errorCode = "INVALID_MONTH", message = "month must be between 1 and 12." });
            }

            var summary = await _attendanceService.GetMonthlySummaryAsync(employeeId, year, month);
            return Ok(summary);
        }

        // ── POST /api/v1/attendance ───────────────────────────────────────────────

        /// <summary>Creates a new attendance record (manual entry by HR/Admin).</summary>
        /// <param name="createDto">Attendance record data.</param>
        /// <response code="201">Record created; Location header points to the new resource.</response>
        /// <response code="400">Validation failed.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Insufficient role.</response>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        [ProducesResponseType(typeof(AttendanceDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateAttendance([FromBody] CreateAttendanceDto createDto)
        {
            AddRateLimitHeaders();

            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var attendance = await _attendanceService.CreateAttendanceAsync(createDto);
            return CreatedAtAction(nameof(GetAttendance), new { id = attendance.Id }, attendance);
        }

        // ── POST /api/v1/attendance/check-in ─────────────────────────────────────

        /// <summary>Records an employee check-in event.</summary>
        /// <param name="checkInDto">Check-in data including employee id and timestamp.</param>
        /// <response code="200">Check-in recorded successfully.</response>
        /// <response code="400">Employee has already checked in today.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Insufficient role.</response>
        [HttpPost("check-in")]
        [Authorize(Roles = "Admin,HR,Manager,Employee")]
        [IgnoreAntiforgeryToken]
        [ProducesResponseType(typeof(AttendanceDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CheckIn([FromBody] CheckInDto checkInDto)
        {
            AddRateLimitHeaders();

            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var alreadyCheckedIn = await _attendanceService.HasCheckedInTodayAsync(checkInDto.EmployeeId);
            if (alreadyCheckedIn)
            {
                return BadRequest(new { errorCode = "ALREADY_CHECKED_IN", message = "Employee has already checked in today." });
            }

            var attendance = await _attendanceService.CheckInAsync(checkInDto);
            return Ok(attendance);
        }

        // ── POST /api/v1/attendance/check-out ─────────────────────────────────────

        /// <summary>Records an employee check-out event.</summary>
        /// <param name="checkOutDto">Check-out data including employee id and timestamp.</param>
        /// <response code="200">Check-out recorded successfully.</response>
        /// <response code="400">Validation failed.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Insufficient role.</response>
        [HttpPost("check-out")]
        [Authorize(Roles = "Admin,HR,Manager,Employee")]
        [IgnoreAntiforgeryToken]
        [ProducesResponseType(typeof(AttendanceDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CheckOut([FromBody] CheckOutDto checkOutDto)
        {
            AddRateLimitHeaders();

            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var attendance = await _attendanceService.CheckOutAsync(checkOutDto);
            return Ok(attendance);
        }

        // ── PUT /api/v1/attendance/{id} ───────────────────────────────────────────

        /// <summary>Updates an existing attendance record.</summary>
        /// <param name="id">The attendance record identifier.</param>
        /// <param name="updateDto">Updated attendance data.</param>
        /// <response code="200">Attendance record updated.</response>
        /// <response code="400">Validation failed or id mismatch.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Insufficient role.</response>
        /// <response code="404">Attendance record not found.</response>
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(AttendanceDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateAttendance(int id, [FromBody] UpdateAttendanceDto updateDto)
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

            var attendance = await _attendanceService.UpdateAttendanceAsync(updateDto);
            return Ok(attendance);
        }

        // ── DELETE /api/v1/attendance/{id} ────────────────────────────────────────

        /// <summary>Deletes an attendance record.</summary>
        /// <param name="id">The attendance record identifier.</param>
        /// <response code="204">Deleted successfully.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Admin role required.</response>
        /// <response code="404">Attendance record not found.</response>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteAttendance(int id)
        {
            AddRateLimitHeaders();

            var deleted = await _attendanceService.DeleteAttendanceAsync(id);
            if (!deleted)
            {
                return NotFound(new { errorCode = "ATTENDANCE_NOT_FOUND", message = $"Attendance record with id {id} was not found." });
            }

            return NoContent();
        }
    }
}
