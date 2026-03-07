using HRMS.Services.Payroll;
using HRMS.Services.Payroll.Dtos;
using HRMS.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Web.Controllers.Api.V1
{
    /// <summary>
    /// RESTful API for payroll processing, salary calculations, and payment management.
    /// </summary>
    /// <remarks>
    /// Payroll lifecycle:
    /// <list type="number">
    ///   <item><c>POST /api/v1/payroll/process</c> – Calculates and records payroll for one employee.</item>
    ///   <item><c>POST /api/v1/payroll/bulk-process</c> – Processes payroll for all active employees (or a department).</item>
    ///   <item><c>POST /api/v1/payroll/{id}/approve</c> – Finance/Admin approves the processed payroll.</item>
    ///   <item><c>POST /api/v1/payroll/{id}/mark-paid</c> – Records the payment after salary disbursement.</item>
    /// </list>
    /// All monetary values are in the local currency and returned as decimal numbers.
    /// </remarks>
    [Authorize(Roles = "Admin,HR")]
    public class PayrollController : ApiControllerBase
    {
        private readonly IPayrollService _payrollService;
        private readonly ILogger<PayrollController> _logger;

        /// <summary>Initialises the controller with its required services.</summary>
        public PayrollController(IPayrollService payrollService, ILogger<PayrollController> logger)
        {
            _payrollService = payrollService;
            _logger = logger;
        }

        // ── GET /api/v1/payroll/{id} ──────────────────────────────────────────────

        /// <summary>Returns a single payroll record by identifier.</summary>
        /// <param name="id">The payroll record identifier.</param>
        /// <response code="200">Payroll record details.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Insufficient role.</response>
        /// <response code="404">Payroll record not found.</response>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(PayrollDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPayroll(int id)
        {
            AddRateLimitHeaders();
            SetNoCache();

            var payroll = await _payrollService.GetPayrollByIdAsync(id);
            if (payroll == null)
            {
                return NotFound(new { errorCode = "PAYROLL_NOT_FOUND", message = $"Payroll record with id {id} was not found." });
            }

            return Ok(payroll);
        }

        // ── GET /api/v1/payroll/employee/{employeeId} ─────────────────────────────

        /// <summary>Returns all payroll records for a specific employee.</summary>
        /// <param name="employeeId">The employee identifier.</param>
        /// <response code="200">Payroll records for the employee.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Insufficient role.</response>
        [HttpGet("employee/{employeeId:int}")]
        [ProducesResponseType(typeof(IEnumerable<PayrollDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetPayrollsByEmployee(int employeeId)
        {
            AddRateLimitHeaders();
            SetNoCache();

            var payrolls = await _payrollService.GetPayrollsByEmployeeAsync(employeeId);
            return Ok(payrolls);
        }

        // ── GET /api/v1/payroll/period ────────────────────────────────────────────

        /// <summary>Returns all payroll records for a given month and year.</summary>
        /// <param name="year">The year (e.g. 2025).</param>
        /// <param name="month">The month (1–12).</param>
        /// <response code="200">Payroll records for the period.</response>
        /// <response code="400">Invalid month.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Insufficient role.</response>
        [HttpGet("period")]
        [ProducesResponseType(typeof(IEnumerable<PayrollDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetPayrollsByPeriod([FromQuery] int year, [FromQuery] int month)
        {
            AddRateLimitHeaders();
            SetNoCache();

            if (month < 1 || month > 12)
            {
                return BadRequest(new { errorCode = "INVALID_MONTH", message = "month must be between 1 and 12." });
            }

            var payrolls = await _payrollService.GetPayrollsByPeriodAsync(year, month);
            return Ok(payrolls);
        }

        // ── GET /api/v1/payroll/period/summary ────────────────────────────────────

        /// <summary>Returns aggregated payroll totals for a given month and year.</summary>
        /// <param name="year">The year.</param>
        /// <param name="month">The month (1–12).</param>
        /// <response code="200">Payroll summary for the period.</response>
        /// <response code="400">Invalid month.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Insufficient role.</response>
        [HttpGet("period/summary")]
        [ProducesResponseType(typeof(PayrollSummaryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetPayrollSummary([FromQuery] int year, [FromQuery] int month)
        {
            AddRateLimitHeaders();
            SetNoCache();

            if (month < 1 || month > 12)
            {
                return BadRequest(new { errorCode = "INVALID_MONTH", message = "month must be between 1 and 12." });
            }

            var summary = await _payrollService.GetPayrollSummaryAsync(year, month);
            return Ok(summary);
        }

        // ── GET /api/v1/payroll/employee/{employeeId}/salary-breakdown ─────────────

        /// <summary>
        /// Returns an estimated salary breakdown for an employee showing all components,
        /// allowances, and deductions based on their current salary.
        /// </summary>
        /// <param name="employeeId">The employee identifier.</param>
        /// <response code="200">Salary breakdown.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Insufficient role.</response>
        /// <response code="404">Employee not found.</response>
        [HttpGet("employee/{employeeId:int}/salary-breakdown")]
        [ProducesResponseType(typeof(SalaryBreakdownDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetSalaryBreakdown(int employeeId)
        {
            AddRateLimitHeaders();
            SetPrivateCache(HrmsConstants.Cache.SlidingExpirationMinutes * 60);

            var breakdown = await _payrollService.GetSalaryBreakdownAsync(employeeId);
            return Ok(breakdown);
        }

        // ── POST /api/v1/payroll/process ──────────────────────────────────────────

        /// <summary>Processes payroll for a single employee for a given month.</summary>
        /// <param name="processDto">Payroll processing parameters.</param>
        /// <response code="201">Payroll record created and processing completed.</response>
        /// <response code="400">Validation failed.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Insufficient role.</response>
        /// <response code="422">Business rule violation (e.g. payroll already processed).</response>
        [HttpPost("process")]
        [ProducesResponseType(typeof(PayrollDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> ProcessPayroll([FromBody] ProcessPayrollDto processDto)
        {
            AddRateLimitHeaders();

            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var payroll = await _payrollService.ProcessPayrollAsync(processDto, processedById: 0);
            _logger.LogInformation("Payroll {PayrollId} processed via API for employee {EmployeeId}",
                payroll.Id, payroll.EmployeeId);

            return CreatedAtAction(nameof(GetPayroll), new { id = payroll.Id }, payroll);
        }

        // ── POST /api/v1/payroll/bulk-process ─────────────────────────────────────

        /// <summary>Bulk-processes payroll for all active employees (or a single department).</summary>
        /// <param name="bulkDto">Bulk processing parameters including period and optional department filter.</param>
        /// <response code="200">Number of payroll records processed.</response>
        /// <response code="400">Validation failed.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Admin role required.</response>
        [HttpPost("bulk-process")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> BulkProcessPayroll([FromBody] BulkProcessPayrollDto bulkDto)
        {
            AddRateLimitHeaders();

            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var count = await _payrollService.BulkProcessPayrollAsync(bulkDto);
            _logger.LogInformation("Bulk payroll processing completed via API: {Count} records processed", count);

            return Ok(new { processedCount = count });
        }

        // ── POST /api/v1/payroll/{id}/approve ─────────────────────────────────────

        /// <summary>Approves a processed payroll record.</summary>
        /// <param name="id">The payroll record identifier.</param>
        /// <param name="approveDto">Approval data.</param>
        /// <response code="200">Payroll approved.</response>
        /// <response code="400">Payroll is not in a processable state or id mismatch.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Admin role required.</response>
        /// <response code="404">Payroll not found.</response>
        [HttpPost("{id:int}/approve")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(PayrollDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ApprovePayroll(int id, [FromBody] ApprovePayrollDto approveDto)
        {
            AddRateLimitHeaders();

            if (id != approveDto.PayrollId)
            {
                return BadRequest(new { errorCode = "ID_MISMATCH", message = "Route id must match body payrollId." });
            }

            var payroll = await _payrollService.ApprovePayrollAsync(approveDto);
            _logger.LogInformation("Payroll {PayrollId} approved via API", id);

            return Ok(payroll);
        }

        // ── POST /api/v1/payroll/{id}/mark-paid ───────────────────────────────────

        /// <summary>Marks a payroll record as paid after salary disbursement.</summary>
        /// <param name="id">The payroll record identifier.</param>
        /// <param name="markAsPaidDto">Payment details including date and reference.</param>
        /// <response code="200">Payroll marked as paid.</response>
        /// <response code="400">Payroll is not in an approved state or id mismatch.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Admin role required.</response>
        /// <response code="404">Payroll not found.</response>
        [HttpPost("{id:int}/mark-paid")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(PayrollDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> MarkAsPaid(int id, [FromBody] MarkAsPaidDto markAsPaidDto)
        {
            AddRateLimitHeaders();

            if (id != markAsPaidDto.PayrollId)
            {
                return BadRequest(new { errorCode = "ID_MISMATCH", message = "Route id must match body payrollId." });
            }

            var payroll = await _payrollService.MarkAsPaidAsync(markAsPaidDto);
            _logger.LogInformation("Payroll {PayrollId} marked as paid via API", id);

            return Ok(payroll);
        }

        // ── DELETE /api/v1/payroll/{id} ───────────────────────────────────────────

        /// <summary>Cancels a payroll record that has not yet been paid.</summary>
        /// <param name="id">The payroll record identifier.</param>
        /// <param name="reason">Optional cancellation reason.</param>
        /// <response code="204">Payroll cancelled successfully.</response>
        /// <response code="400">Payroll cannot be cancelled (e.g. already paid).</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Admin role required.</response>
        /// <response code="404">Payroll not found.</response>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CancelPayroll(int id, [FromQuery] string? reason)
        {
            AddRateLimitHeaders();

            var cancelled = await _payrollService.CancelPayrollAsync(id, reason);
            if (!cancelled)
            {
                return BadRequest(new
                {
                    errorCode = "PAYROLL_CANCEL_FAILED",
                    message = "Payroll could not be cancelled. It may already be paid or not exist."
                });
            }

            _logger.LogInformation("Payroll {PayrollId} cancelled via API", id);
            return NoContent();
        }
    }
}
