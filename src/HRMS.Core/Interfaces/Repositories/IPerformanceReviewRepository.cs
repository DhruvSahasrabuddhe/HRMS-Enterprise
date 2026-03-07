using HRMS.Core.Entities;
using HRMS.Core.Enums;

namespace HRMS.Core.Interfaces.Repositories
{
    /// <summary>
    /// Repository interface for performance review data access.
    /// </summary>
    public interface IPerformanceReviewRepository : IGenericRepository<PerformanceReview>
    {
        /// <summary>Gets all reviews for an employee.</summary>
        Task<IEnumerable<PerformanceReview>> GetReviewsByEmployeeAsync(int employeeId);

        /// <summary>Gets all reviews submitted by a specific reviewer.</summary>
        Task<IEnumerable<PerformanceReview>> GetReviewsByReviewerAsync(int reviewerId);

        /// <summary>Gets reviews by status.</summary>
        Task<IEnumerable<PerformanceReview>> GetReviewsByStatusAsync(PerformanceReviewStatus status);

        /// <summary>Gets reviews for a specific year and cycle type.</summary>
        Task<IEnumerable<PerformanceReview>> GetReviewsByCycleAsync(int year, ReviewCycleType cycleType);

        /// <summary>Gets pending reviews due before a given date.</summary>
        Task<IEnumerable<PerformanceReview>> GetPendingReviewsAsync(DateTime dueDate);

        /// <summary>Gets a review with its navigation properties loaded.</summary>
        Task<PerformanceReview?> GetReviewWithDetailsAsync(int reviewId);

        /// <summary>Checks whether an active review already exists for an employee in a given cycle.</summary>
        Task<bool> HasActiveReviewAsync(int employeeId, int year, ReviewCycleType cycleType);

        /// <summary>Gets the average rating score for an employee across completed reviews.</summary>
        Task<double?> GetAverageRatingAsync(int employeeId);
    }
}
