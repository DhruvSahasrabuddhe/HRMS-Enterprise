using HRMS.Core.Enums;
using HRMS.Services.PerformanceReviews;
using HRMS.Services.PerformanceReviews.Dtos;
using HRMS.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Web.Controllers.Api.V1
{
    /// <summary>
    /// RESTful API for managing employee performance review cycles.
    /// </summary>
    /// <remarks>
    /// Review lifecycle:
    /// <list type="number">
    ///   <item>HR creates a review: <c>POST /api/v1/performance-reviews</c>.</item>
    ///   <item>Employee submits self-assessment: <c>POST /api/v1/performance-reviews/{id}/self-assessment</c>.</item>
    ///   <item>Manager submits review: <c>POST /api/v1/performance-reviews/{id}/manager-review</c>.</item>
    ///   <item>HR finalizes the review: <c>POST /api/v1/performance-reviews/{id}/finalize</c>.</item>
    ///   <item>Employee acknowledges: <c>POST /api/v1/performance-reviews/{id}/acknowledge</c>.</item>
    /// </list>
    /// Ratings use the <c>PerformanceRating</c> enum (1–5 scale).
    /// </remarks>
    [Authorize(Roles = "Admin,HR,Manager")]
    [Route(HrmsConstants.Api.RoutePrefix + "/performance-reviews")]
    public class PerformanceReviewsController : ApiControllerBase
    {
        private readonly IPerformanceReviewService _reviewService;
        private readonly ILogger<PerformanceReviewsController> _logger;

        /// <summary>Initializes the controller with its required services.</summary>
        public PerformanceReviewsController(
            IPerformanceReviewService reviewService,
            ILogger<PerformanceReviewsController> logger)
        {
            _reviewService = reviewService;
            _logger = logger;
        }

        // ── GET /api/v1/performance-reviews/{id} ──────────────────────────────────

        /// <summary>Returns a single performance review by identifier.</summary>
        /// <param name="id">The performance review identifier.</param>
        /// <response code="200">Performance review details.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Insufficient role.</response>
        /// <response code="404">Review not found.</response>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(PerformanceReviewDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetReview(int id)
        {
            AddRateLimitHeaders();
            SetNoCache();

            var review = await _reviewService.GetReviewByIdAsync(id);
            if (review == null)
            {
                return NotFound(new { errorCode = "REVIEW_NOT_FOUND", message = $"Performance review with id {id} was not found." });
            }

            return Ok(review);
        }

        // ── GET /api/v1/performance-reviews/employee/{employeeId} ────────────────

        /// <summary>Returns all performance reviews for a specific employee.</summary>
        /// <param name="employeeId">The employee identifier.</param>
        /// <response code="200">Performance reviews for the employee.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Insufficient role.</response>
        [HttpGet("employee/{employeeId:int}")]
        [ProducesResponseType(typeof(IEnumerable<PerformanceReviewDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetReviewsByEmployee(int employeeId)
        {
            AddRateLimitHeaders();
            SetNoCache();

            var reviews = await _reviewService.GetReviewsByEmployeeAsync(employeeId);
            return Ok(reviews);
        }

        // ── GET /api/v1/performance-reviews/employee/{employeeId}/summary ─────────

        /// <summary>Returns a review history summary and average rating for an employee.</summary>
        /// <param name="employeeId">The employee identifier.</param>
        /// <response code="200">Review summary.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Insufficient role.</response>
        [HttpGet("employee/{employeeId:int}/summary")]
        [ProducesResponseType(typeof(ReviewSummaryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetEmployeeSummary(int employeeId)
        {
            AddRateLimitHeaders();
            SetNoCache();

            var summary = await _reviewService.GetEmployeeReviewSummaryAsync(employeeId);
            return Ok(summary);
        }

        // ── GET /api/v1/performance-reviews/overdue ───────────────────────────────

        /// <summary>Returns all performance reviews that are past their due date and not yet completed.</summary>
        /// <response code="200">List of overdue reviews.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Insufficient role.</response>
        [HttpGet("overdue")]
        [ProducesResponseType(typeof(IEnumerable<PerformanceReviewDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetOverdueReviews()
        {
            AddRateLimitHeaders();
            SetNoCache();

            var reviews = await _reviewService.GetOverdueReviewsAsync();
            return Ok(reviews);
        }

        // ── POST /api/v1/performance-reviews ─────────────────────────────────────

        /// <summary>Creates a new performance review cycle for an employee.</summary>
        /// <param name="createDto">Review data.</param>
        /// <response code="201">Review created; Location header points to the new resource.</response>
        /// <response code="400">Validation failed.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Insufficient role.</response>
        [HttpPost]
        [ProducesResponseType(typeof(PerformanceReviewDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateReview([FromBody] CreatePerformanceReviewDto createDto)
        {
            AddRateLimitHeaders();

            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var review = await _reviewService.CreateReviewAsync(createDto);
            _logger.LogInformation("Performance review {ReviewId} created via API for employee {EmployeeId}",
                review.Id, review.EmployeeId);

            return CreatedAtAction(nameof(GetReview), new { id = review.Id }, review);
        }

        // ── PUT /api/v1/performance-reviews/{id} ──────────────────────────────────

        /// <summary>Updates review metadata such as due date, goals, or development plan.</summary>
        /// <param name="id">The review identifier.</param>
        /// <param name="updateDto">Updated review data.</param>
        /// <response code="200">Review updated.</response>
        /// <response code="400">Validation failed or id mismatch.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Insufficient role.</response>
        /// <response code="404">Review not found.</response>
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(PerformanceReviewDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateReview(int id, [FromBody] UpdatePerformanceReviewDto updateDto)
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

            var review = await _reviewService.UpdateReviewAsync(updateDto);
            return Ok(review);
        }

        // ── POST /api/v1/performance-reviews/{id}/self-assessment ─────────────────

        /// <summary>Records an employee's self-assessment for a review.</summary>
        /// <param name="id">The review identifier.</param>
        /// <param name="dto">Self-assessment data.</param>
        /// <response code="200">Self-assessment submitted.</response>
        /// <response code="400">Validation failed or id mismatch.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Insufficient role.</response>
        /// <response code="404">Review not found.</response>
        [HttpPost("{id:int}/self-assessment")]
        [Authorize(Roles = "Admin,HR,Manager,Employee")]
        [ProducesResponseType(typeof(PerformanceReviewDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SubmitSelfAssessment(int id, [FromBody] SelfAssessmentDto dto)
        {
            AddRateLimitHeaders();

            if (id != dto.ReviewId)
            {
                return BadRequest(new { errorCode = "ID_MISMATCH", message = "Route id must match body reviewId." });
            }

            var review = await _reviewService.SubmitSelfAssessmentAsync(dto);
            return Ok(review);
        }

        // ── POST /api/v1/performance-reviews/{id}/manager-review ──────────────────

        /// <summary>Records the manager's review scores and comments.</summary>
        /// <param name="id">The review identifier.</param>
        /// <param name="dto">Manager review data.</param>
        /// <response code="200">Manager review submitted.</response>
        /// <response code="400">Validation failed or id mismatch.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Manager or Admin/HR role required.</response>
        /// <response code="404">Review not found.</response>
        [HttpPost("{id:int}/manager-review")]
        [ProducesResponseType(typeof(PerformanceReviewDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SubmitManagerReview(int id, [FromBody] ManagerReviewDto dto)
        {
            AddRateLimitHeaders();

            if (id != dto.ReviewId)
            {
                return BadRequest(new { errorCode = "ID_MISMATCH", message = "Route id must match body reviewId." });
            }

            var review = await _reviewService.SubmitManagerReviewAsync(dto);
            return Ok(review);
        }

        // ── POST /api/v1/performance-reviews/{id}/finalize ────────────────────────

        /// <summary>Finalizes a performance review with overall rating and HR comments.</summary>
        /// <param name="id">The review identifier.</param>
        /// <param name="dto">Finalization data.</param>
        /// <response code="200">Review finalized.</response>
        /// <response code="400">Validation failed or id mismatch.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">HR or Admin role required.</response>
        /// <response code="404">Review not found.</response>
        [HttpPost("{id:int}/finalize")]
        [Authorize(Roles = "Admin,HR")]
        [ProducesResponseType(typeof(PerformanceReviewDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> FinalizeReview(int id, [FromBody] FinalizeReviewDto dto)
        {
            AddRateLimitHeaders();

            if (id != dto.ReviewId)
            {
                return BadRequest(new { errorCode = "ID_MISMATCH", message = "Route id must match body reviewId." });
            }

            var review = await _reviewService.FinalizeReviewAsync(dto);
            _logger.LogInformation("Performance review {ReviewId} finalized via API", id);

            return Ok(review);
        }

        // ── POST /api/v1/performance-reviews/{id}/acknowledge ─────────────────────

        /// <summary>Marks a finalized review as acknowledged by the employee.</summary>
        /// <param name="id">The review identifier.</param>
        /// <param name="employeeId">The identifier of the employee acknowledging the review.</param>
        /// <response code="200">Review acknowledged.</response>
        /// <response code="400">Review is not in a finalized state.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Insufficient role.</response>
        /// <response code="404">Review not found.</response>
        [HttpPost("{id:int}/acknowledge")]
        [Authorize(Roles = "Admin,HR,Manager,Employee")]
        [ProducesResponseType(typeof(PerformanceReviewDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AcknowledgeReview(int id, [FromQuery] int employeeId)
        {
            AddRateLimitHeaders();

            var review = await _reviewService.AcknowledgeReviewAsync(id, employeeId);
            return Ok(review);
        }

        // ── DELETE /api/v1/performance-reviews/{id} ───────────────────────────────

        /// <summary>Deletes a draft performance review.</summary>
        /// <param name="id">The review identifier.</param>
        /// <response code="204">Review deleted.</response>
        /// <response code="400">Cannot delete a review that is not in draft status.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Admin role required.</response>
        /// <response code="404">Review not found.</response>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteReview(int id)
        {
            AddRateLimitHeaders();

            var deleted = await _reviewService.DeleteReviewAsync(id);
            if (!deleted)
            {
                return NotFound(new { errorCode = "REVIEW_NOT_FOUND", message = $"Performance review with id {id} was not found." });
            }

            _logger.LogInformation("Performance review {ReviewId} deleted via API", id);
            return NoContent();
        }
    }
}
