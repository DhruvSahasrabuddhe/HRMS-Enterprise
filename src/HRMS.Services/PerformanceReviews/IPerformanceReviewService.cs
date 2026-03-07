using HRMS.Core.Enums;
using HRMS.Services.PerformanceReviews.Dtos;

namespace HRMS.Services.PerformanceReviews
{
    /// <summary>
    /// Service interface for managing employee performance review cycles.
    /// </summary>
    public interface IPerformanceReviewService
    {
        /// <summary>Gets a performance review by ID with full details.</summary>
        Task<PerformanceReviewDto?> GetReviewByIdAsync(int id);

        /// <summary>Gets all reviews for an employee.</summary>
        Task<IEnumerable<PerformanceReviewDto>> GetReviewsByEmployeeAsync(int employeeId);

        /// <summary>Gets all reviews assigned to a specific reviewer.</summary>
        Task<IEnumerable<PerformanceReviewDto>> GetReviewsByReviewerAsync(int reviewerId);

        /// <summary>Gets reviews filtered by status.</summary>
        Task<IEnumerable<PerformanceReviewDto>> GetReviewsByStatusAsync(PerformanceReviewStatus status);

        /// <summary>Gets reviews for a specific year and cycle type.</summary>
        Task<IEnumerable<PerformanceReviewDto>> GetReviewsByCycleAsync(int year, ReviewCycleType cycleType);

        /// <summary>Gets reviews that are overdue (past due date and not completed).</summary>
        Task<IEnumerable<PerformanceReviewDto>> GetOverdueReviewsAsync();

        /// <summary>Creates a new performance review cycle for an employee.</summary>
        Task<PerformanceReviewDto> CreateReviewAsync(CreatePerformanceReviewDto createDto);

        /// <summary>Updates review metadata such as due date or goals.</summary>
        Task<PerformanceReviewDto> UpdateReviewAsync(UpdatePerformanceReviewDto updateDto);

        /// <summary>Records an employee's self-assessment.</summary>
        Task<PerformanceReviewDto> SubmitSelfAssessmentAsync(SelfAssessmentDto dto);

        /// <summary>Records the manager review scores and comments.</summary>
        Task<PerformanceReviewDto> SubmitManagerReviewAsync(ManagerReviewDto dto);

        /// <summary>Finalizes the review with overall rating and HR comments.</summary>
        Task<PerformanceReviewDto> FinalizeReviewAsync(FinalizeReviewDto dto);

        /// <summary>Marks the review as acknowledged by the employee.</summary>
        Task<PerformanceReviewDto> AcknowledgeReviewAsync(int reviewId, int employeeId);

        /// <summary>Deletes a draft review.</summary>
        Task<bool> DeleteReviewAsync(int id);

        /// <summary>Gets a summary of review history and average rating for an employee.</summary>
        Task<ReviewSummaryDto> GetEmployeeReviewSummaryAsync(int employeeId);
    }
}
