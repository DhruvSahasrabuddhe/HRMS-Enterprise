using HRMS.Core.Entities;
using HRMS.Core.Enums;
using HRMS.Core.Interfaces.Repositories;
using HRMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Infrastructure.Repositories
{
    public class PerformanceReviewRepository : GenericRepository<PerformanceReview>, IPerformanceReviewRepository
    {
        public PerformanceReviewRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<PerformanceReview>> GetReviewsByEmployeeAsync(int employeeId)
        {
            return await _dbSet
                .Where(r => r.EmployeeId == employeeId)
                .Include(r => r.Employee)
                .Include(r => r.Reviewer)
                .OrderByDescending(r => r.ReviewYear)
                .ThenByDescending(r => r.ReviewPeriodMonth)
                .ToListAsync();
        }

        public async Task<IEnumerable<PerformanceReview>> GetReviewsByReviewerAsync(int reviewerId)
        {
            return await _dbSet
                .Where(r => r.ReviewerId == reviewerId)
                .Include(r => r.Employee)
                .Include(r => r.Reviewer)
                .OrderByDescending(r => r.DueDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<PerformanceReview>> GetReviewsByStatusAsync(PerformanceReviewStatus status)
        {
            return await _dbSet
                .Where(r => r.Status == status)
                .Include(r => r.Employee)
                .Include(r => r.Reviewer)
                .OrderBy(r => r.DueDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<PerformanceReview>> GetReviewsByCycleAsync(int year, ReviewCycleType cycleType)
        {
            return await _dbSet
                .Where(r => r.ReviewYear == year && r.CycleType == cycleType)
                .Include(r => r.Employee)
                .Include(r => r.Reviewer)
                .OrderBy(r => r.Employee.LastName)
                .ToListAsync();
        }

        public async Task<IEnumerable<PerformanceReview>> GetPendingReviewsAsync(DateTime dueDate)
        {
            return await _dbSet
                .Where(r => r.DueDate <= dueDate
                         && r.Status != PerformanceReviewStatus.Completed
                         && r.Status != PerformanceReviewStatus.Acknowledged)
                .Include(r => r.Employee)
                .Include(r => r.Reviewer)
                .OrderBy(r => r.DueDate)
                .ToListAsync();
        }

        public async Task<PerformanceReview?> GetReviewWithDetailsAsync(int reviewId)
        {
            return await _dbSet
                .Include(r => r.Employee)
                    .ThenInclude(e => e.Department)
                .Include(r => r.Reviewer)
                .FirstOrDefaultAsync(r => r.Id == reviewId);
        }

        public async Task<bool> HasActiveReviewAsync(int employeeId, int year, ReviewCycleType cycleType)
        {
            return await _dbSet
                .AnyAsync(r => r.EmployeeId == employeeId
                            && r.ReviewYear == year
                            && r.CycleType == cycleType
                            && r.Status != PerformanceReviewStatus.Completed
                            && r.Status != PerformanceReviewStatus.Acknowledged);
        }

        public async Task<double?> GetAverageRatingAsync(int employeeId)
        {
            var ratings = await _dbSet
                .Where(r => r.EmployeeId == employeeId
                         && r.Status == PerformanceReviewStatus.Completed
                         && r.OverallRating.HasValue)
                .Select(r => (int)r.OverallRating!.Value)
                .ToListAsync();

            if (ratings.Count == 0) return null;
            return ratings.Average();
        }
    }
}
