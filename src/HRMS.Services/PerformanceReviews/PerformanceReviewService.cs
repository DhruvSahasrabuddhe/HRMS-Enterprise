using AutoMapper;
using HRMS.Core.Enums;
using HRMS.Core.Interfaces.Repositories;
using HRMS.Core.Interfaces.Services;
using HRMS.Services.PerformanceReviews.Dtos;
using HRMS.Shared.Constants;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace HRMS.Services.PerformanceReviews
{
    /// <summary>
    /// Service for managing employee performance review cycles.
    /// </summary>
    public class PerformanceReviewService : IPerformanceReviewService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<PerformanceReviewService> _logger;
        private readonly IMemoryCache _cache;
        private readonly IDateTimeProvider _dateTimeProvider;

        public PerformanceReviewService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<PerformanceReviewService> logger,
            IMemoryCache cache,
            IDateTimeProvider dateTimeProvider)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _cache = cache;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<PerformanceReviewDto?> GetReviewByIdAsync(int id)
        {
            try
            {
                var cacheKey = HrmsConstants.Performance.ReviewKey(id);
                if (_cache.TryGetValue(cacheKey, out PerformanceReviewDto? cached))
                    return cached;

                var review = await _unitOfWork.PerformanceReviews.GetReviewWithDetailsAsync(id);
                if (review == null) return null;

                var dto = _mapper.Map<PerformanceReviewDto>(review);
                _cache.Set(cacheKey, dto, TimeSpan.FromMinutes(HrmsConstants.Cache.DefaultExpirationMinutes));
                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting review {ReviewId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<PerformanceReviewDto>> GetReviewsByEmployeeAsync(int employeeId)
        {
            try
            {
                var reviews = await _unitOfWork.PerformanceReviews.GetReviewsByEmployeeAsync(employeeId);
                return _mapper.Map<IEnumerable<PerformanceReviewDto>>(reviews);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reviews for employee {EmployeeId}", employeeId);
                throw;
            }
        }

        public async Task<IEnumerable<PerformanceReviewDto>> GetReviewsByReviewerAsync(int reviewerId)
        {
            try
            {
                var reviews = await _unitOfWork.PerformanceReviews.GetReviewsByReviewerAsync(reviewerId);
                return _mapper.Map<IEnumerable<PerformanceReviewDto>>(reviews);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reviews for reviewer {ReviewerId}", reviewerId);
                throw;
            }
        }

        public async Task<IEnumerable<PerformanceReviewDto>> GetReviewsByStatusAsync(PerformanceReviewStatus status)
        {
            try
            {
                var reviews = await _unitOfWork.PerformanceReviews.GetReviewsByStatusAsync(status);
                return _mapper.Map<IEnumerable<PerformanceReviewDto>>(reviews);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reviews by status {Status}", status);
                throw;
            }
        }

        public async Task<IEnumerable<PerformanceReviewDto>> GetReviewsByCycleAsync(int year, ReviewCycleType cycleType)
        {
            try
            {
                var reviews = await _unitOfWork.PerformanceReviews.GetReviewsByCycleAsync(year, cycleType);
                return _mapper.Map<IEnumerable<PerformanceReviewDto>>(reviews);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reviews for cycle {Year}/{CycleType}", year, cycleType);
                throw;
            }
        }

        public async Task<IEnumerable<PerformanceReviewDto>> GetOverdueReviewsAsync()
        {
            try
            {
                var reviews = await _unitOfWork.PerformanceReviews.GetPendingReviewsAsync(_dateTimeProvider.UtcNow);
                return _mapper.Map<IEnumerable<PerformanceReviewDto>>(reviews);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting overdue reviews");
                throw;
            }
        }

        public async Task<PerformanceReviewDto> CreateReviewAsync(CreatePerformanceReviewDto createDto)
        {
            try
            {
                _logger.LogInformation("Creating performance review for employee {EmployeeId}", createDto.EmployeeId);

                var employee = await _unitOfWork.Employees.GetByIdAsync(createDto.EmployeeId);
                if (employee == null)
                    throw new KeyNotFoundException($"Employee with ID {createDto.EmployeeId} not found");

                var reviewer = await _unitOfWork.Employees.GetByIdAsync(createDto.ReviewerId);
                if (reviewer == null)
                    throw new KeyNotFoundException($"Reviewer with ID {createDto.ReviewerId} not found");

                var hasActive = await _unitOfWork.PerformanceReviews.HasActiveReviewAsync(
                    createDto.EmployeeId, createDto.ReviewYear, createDto.CycleType);
                if (hasActive)
                    throw new InvalidOperationException(
                        $"An active {createDto.CycleType} review already exists for employee {createDto.EmployeeId} in {createDto.ReviewYear}");

                var review = _mapper.Map<Core.Entities.PerformanceReview>(createDto);
                review.Status = PerformanceReviewStatus.Draft;
                review.CreatedAt = _dateTimeProvider.UtcNow;

                await _unitOfWork.PerformanceReviews.AddAsync(review);
                await _unitOfWork.CompleteAsync();

                _logger.LogInformation("Performance review created with ID {ReviewId}", review.Id);
                InvalidateEmployeeReviewCache(createDto.EmployeeId);

                var created = await _unitOfWork.PerformanceReviews.GetReviewWithDetailsAsync(review.Id);
                return _mapper.Map<PerformanceReviewDto>(created!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating performance review");
                throw;
            }
        }

        public async Task<PerformanceReviewDto> UpdateReviewAsync(UpdatePerformanceReviewDto updateDto)
        {
            try
            {
                var review = await _unitOfWork.PerformanceReviews.GetByIdAsync(updateDto.Id);
                if (review == null)
                    throw new KeyNotFoundException($"Review with ID {updateDto.Id} not found");

                if (review.Status == PerformanceReviewStatus.Completed || review.Status == PerformanceReviewStatus.Acknowledged)
                    throw new InvalidOperationException("Cannot update a completed or acknowledged review");

                if (updateDto.GoalsSet != null) review.GoalsSet = updateDto.GoalsSet;
                if (updateDto.DevelopmentPlan != null) review.DevelopmentPlan = updateDto.DevelopmentPlan;
                if (updateDto.DueDate.HasValue) review.DueDate = updateDto.DueDate.Value;
                review.UpdatedAt = _dateTimeProvider.UtcNow;

                _unitOfWork.PerformanceReviews.Update(review);
                await _unitOfWork.CompleteAsync();

                _cache.Remove(HrmsConstants.Performance.ReviewKey(review.Id));
                InvalidateEmployeeReviewCache(review.EmployeeId);

                var updated = await _unitOfWork.PerformanceReviews.GetReviewWithDetailsAsync(review.Id);
                return _mapper.Map<PerformanceReviewDto>(updated!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating review {ReviewId}", updateDto.Id);
                throw;
            }
        }

        public async Task<PerformanceReviewDto> SubmitSelfAssessmentAsync(SelfAssessmentDto dto)
        {
            try
            {
                var review = await _unitOfWork.PerformanceReviews.GetByIdAsync(dto.ReviewId);
                if (review == null)
                    throw new KeyNotFoundException($"Review with ID {dto.ReviewId} not found");

                if (review.Status == PerformanceReviewStatus.Completed || review.Status == PerformanceReviewStatus.Acknowledged)
                    throw new InvalidOperationException("Cannot submit self-assessment for a completed review");

                review.SelfRating = dto.SelfRating;
                review.SelfComments = dto.SelfComments;
                review.GoalsAchieved = dto.GoalsAchieved;
                review.SelfAssessmentDate = _dateTimeProvider.UtcNow;
                review.Status = PerformanceReviewStatus.ManagerReviewPending;
                review.UpdatedAt = _dateTimeProvider.UtcNow;

                _unitOfWork.PerformanceReviews.Update(review);
                await _unitOfWork.CompleteAsync();

                _cache.Remove(HrmsConstants.Performance.ReviewKey(review.Id));
                InvalidateEmployeeReviewCache(review.EmployeeId);

                var updated = await _unitOfWork.PerformanceReviews.GetReviewWithDetailsAsync(review.Id);
                return _mapper.Map<PerformanceReviewDto>(updated!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting self-assessment for review {ReviewId}", dto.ReviewId);
                throw;
            }
        }

        public async Task<PerformanceReviewDto> SubmitManagerReviewAsync(ManagerReviewDto dto)
        {
            try
            {
                var review = await _unitOfWork.PerformanceReviews.GetByIdAsync(dto.ReviewId);
                if (review == null)
                    throw new KeyNotFoundException($"Review with ID {dto.ReviewId} not found");

                if (review.Status != PerformanceReviewStatus.ManagerReviewPending &&
                    review.Status != PerformanceReviewStatus.Draft)
                    throw new InvalidOperationException("Review is not in a state that allows manager review");

                review.ManagerRating = dto.ManagerRating;
                review.ManagerComments = dto.ManagerComments;
                review.ManagerReviewDate = _dateTimeProvider.UtcNow;
                review.TargetAchievementScore = dto.TargetAchievementScore;
                review.CompetencyScore = dto.CompetencyScore;
                if (dto.DevelopmentPlan != null) review.DevelopmentPlan = dto.DevelopmentPlan;
                review.Status = PerformanceReviewStatus.HrReviewPending;

                // Calculate overall score
                if (dto.TargetAchievementScore.HasValue && dto.CompetencyScore.HasValue)
                {
                    review.OverallScore =
                        dto.TargetAchievementScore.Value * HrmsConstants.Performance.TargetAchievementWeight
                        + dto.CompetencyScore.Value * HrmsConstants.Performance.CompetencyWeight;
                }

                review.UpdatedAt = _dateTimeProvider.UtcNow;

                _unitOfWork.PerformanceReviews.Update(review);
                await _unitOfWork.CompleteAsync();

                _cache.Remove(HrmsConstants.Performance.ReviewKey(review.Id));
                InvalidateEmployeeReviewCache(review.EmployeeId);

                var updated = await _unitOfWork.PerformanceReviews.GetReviewWithDetailsAsync(review.Id);
                return _mapper.Map<PerformanceReviewDto>(updated!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting manager review for review {ReviewId}", dto.ReviewId);
                throw;
            }
        }

        public async Task<PerformanceReviewDto> FinalizeReviewAsync(FinalizeReviewDto dto)
        {
            try
            {
                var review = await _unitOfWork.PerformanceReviews.GetByIdAsync(dto.ReviewId);
                if (review == null)
                    throw new KeyNotFoundException($"Review with ID {dto.ReviewId} not found");

                if (review.Status != PerformanceReviewStatus.HrReviewPending &&
                    review.Status != PerformanceReviewStatus.ManagerReviewPending)
                    throw new InvalidOperationException("Review is not ready to be finalized");

                review.OverallRating = dto.OverallRating;
                review.OverallComments = dto.OverallComments;
                review.HrComments = dto.HrComments;
                review.HrReviewDate = _dateTimeProvider.UtcNow;
                review.Status = PerformanceReviewStatus.Completed;
                review.UpdatedAt = _dateTimeProvider.UtcNow;

                _unitOfWork.PerformanceReviews.Update(review);
                await _unitOfWork.CompleteAsync();

                _cache.Remove(HrmsConstants.Performance.ReviewKey(review.Id));
                InvalidateEmployeeReviewCache(review.EmployeeId);

                var updated = await _unitOfWork.PerformanceReviews.GetReviewWithDetailsAsync(review.Id);
                return _mapper.Map<PerformanceReviewDto>(updated!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finalizing review {ReviewId}", dto.ReviewId);
                throw;
            }
        }

        public async Task<PerformanceReviewDto> AcknowledgeReviewAsync(int reviewId, int employeeId)
        {
            try
            {
                var review = await _unitOfWork.PerformanceReviews.GetByIdAsync(reviewId);
                if (review == null)
                    throw new KeyNotFoundException($"Review with ID {reviewId} not found");

                if (review.EmployeeId != employeeId)
                    throw new InvalidOperationException("Employee can only acknowledge their own review");

                if (review.Status != PerformanceReviewStatus.Completed)
                    throw new InvalidOperationException("Only completed reviews can be acknowledged");

                review.AcknowledgedDate = _dateTimeProvider.UtcNow;
                review.Status = PerformanceReviewStatus.Acknowledged;
                review.UpdatedAt = _dateTimeProvider.UtcNow;

                _unitOfWork.PerformanceReviews.Update(review);
                await _unitOfWork.CompleteAsync();

                _cache.Remove(HrmsConstants.Performance.ReviewKey(review.Id));
                InvalidateEmployeeReviewCache(review.EmployeeId);

                var updated = await _unitOfWork.PerformanceReviews.GetReviewWithDetailsAsync(review.Id);
                return _mapper.Map<PerformanceReviewDto>(updated!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error acknowledging review {ReviewId}", reviewId);
                throw;
            }
        }

        public async Task<bool> DeleteReviewAsync(int id)
        {
            try
            {
                var review = await _unitOfWork.PerformanceReviews.GetByIdAsync(id);
                if (review == null)
                    throw new KeyNotFoundException($"Review with ID {id} not found");

                if (review.Status != PerformanceReviewStatus.Draft)
                    throw new InvalidOperationException("Only draft reviews can be deleted");

                review.IsDeleted = true;
                review.UpdatedAt = _dateTimeProvider.UtcNow;

                _unitOfWork.PerformanceReviews.Update(review);
                await _unitOfWork.CompleteAsync();

                _cache.Remove(HrmsConstants.Performance.ReviewKey(id));
                InvalidateEmployeeReviewCache(review.EmployeeId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting review {ReviewId}", id);
                throw;
            }
        }

        public async Task<ReviewSummaryDto> GetEmployeeReviewSummaryAsync(int employeeId)
        {
            try
            {
                var employee = await _unitOfWork.Employees.GetByIdAsync(employeeId);
                if (employee == null)
                    throw new KeyNotFoundException($"Employee with ID {employeeId} not found");

                var reviews = (await _unitOfWork.PerformanceReviews.GetReviewsByEmployeeAsync(employeeId)).ToList();
                var avgRating = await _unitOfWork.PerformanceReviews.GetAverageRatingAsync(employeeId);
                var latest = reviews.OrderByDescending(r => r.ReviewYear)
                    .ThenByDescending(r => r.ReviewPeriodMonth)
                    .FirstOrDefault(r => r.Status == PerformanceReviewStatus.Completed || r.Status == PerformanceReviewStatus.Acknowledged);

                return new ReviewSummaryDto
                {
                    EmployeeId = employeeId,
                    EmployeeName = employee.FullName,
                    TotalReviews = reviews.Count,
                    AverageRating = avgRating,
                    LatestRating = latest?.OverallRating,
                    PendingReviews = reviews.Count(r =>
                        r.Status != PerformanceReviewStatus.Completed &&
                        r.Status != PerformanceReviewStatus.Acknowledged)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting review summary for employee {EmployeeId}", employeeId);
                throw;
            }
        }

        private void InvalidateEmployeeReviewCache(int employeeId)
        {
            _cache.Remove(HrmsConstants.Performance.EmployeeReviewsKey(employeeId));
        }
    }
}
