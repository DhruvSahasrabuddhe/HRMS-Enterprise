using AutoMapper;
using HRMS.Core.Entities;
using HRMS.Core.Enums;
using HRMS.Core.Interfaces.Repositories;
using HRMS.Core.Interfaces.Services;
using HRMS.Services.Mappings;
using HRMS.Services.PerformanceReviews;
using HRMS.Services.PerformanceReviews.Dtos;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace HRMS.UnitTests.Services
{
    public class PerformanceReviewServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IPerformanceReviewRepository> _reviewRepoMock;
        private readonly Mock<IEmployeeRepository> _employeeRepoMock;
        private readonly IMapper _mapper;
        private readonly Mock<ILogger<PerformanceReviewService>> _loggerMock;
        private readonly IMemoryCache _cache;
        private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;
        private readonly PerformanceReviewService _sut;

        private static readonly DateTime FixedNow = new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc);

        public PerformanceReviewServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _reviewRepoMock = new Mock<IPerformanceReviewRepository>();
            _employeeRepoMock = new Mock<IEmployeeRepository>();
            _loggerMock = new Mock<ILogger<PerformanceReviewService>>();
            _dateTimeProviderMock = new Mock<IDateTimeProvider>();

            _unitOfWorkMock.Setup(u => u.PerformanceReviews).Returns(_reviewRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.Employees).Returns(_employeeRepoMock.Object);
            _dateTimeProviderMock.Setup(d => d.UtcNow).Returns(FixedNow);

            var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
            _mapper = config.CreateMapper();
            _cache = new MemoryCache(new MemoryCacheOptions());

            _sut = new PerformanceReviewService(
                _unitOfWorkMock.Object,
                _mapper,
                _loggerMock.Object,
                _cache,
                _dateTimeProviderMock.Object);
        }

        [Fact]
        public async Task GetReviewByIdAsync_WhenExists_ReturnsDto()
        {
            // Arrange
            var employee = BuildEmployee(1);
            var reviewer = BuildEmployee(2);
            var review = BuildReview(1, employee, reviewer);

            _reviewRepoMock.Setup(r => r.GetReviewWithDetailsAsync(1)).ReturnsAsync(review);

            // Act
            var result = await _sut.GetReviewByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal(PerformanceReviewStatus.Draft, result.Status);
            Assert.Equal("Test User", result.EmployeeName);
        }

        [Fact]
        public async Task GetReviewByIdAsync_WhenNotFound_ReturnsNull()
        {
            _reviewRepoMock.Setup(r => r.GetReviewWithDetailsAsync(999)).ReturnsAsync((PerformanceReview?)null);

            var result = await _sut.GetReviewByIdAsync(999);

            Assert.Null(result);
        }

        [Fact]
        public async Task CreateReviewAsync_WithValidData_CreatesReview()
        {
            // Arrange
            var createDto = new CreatePerformanceReviewDto
            {
                EmployeeId = 1,
                ReviewerId = 2,
                Title = "Annual Review 2025",
                CycleType = ReviewCycleType.Annual,
                ReviewYear = 2025,
                ReviewPeriodMonth = 1,
                ReviewStartDate = new DateTime(2025, 1, 1),
                ReviewEndDate = new DateTime(2025, 12, 31),
                DueDate = new DateTime(2025, 2, 15)
            };

            var employee = BuildEmployee(1);
            var reviewer = BuildEmployee(2);
            var createdReview = BuildReview(10, employee, reviewer);
            createdReview.Title = createDto.Title;
            createdReview.CycleType = ReviewCycleType.Annual;
            createdReview.ReviewYear = 2025;

            _employeeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(employee);
            _employeeRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(reviewer);
            _reviewRepoMock
                .Setup(r => r.HasActiveReviewAsync(1, 2025, ReviewCycleType.Annual))
                .ReturnsAsync(false);
            _reviewRepoMock.Setup(r => r.AddAsync(It.IsAny<PerformanceReview>())).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(1);
            _reviewRepoMock.Setup(r => r.GetReviewWithDetailsAsync(It.IsAny<int>())).ReturnsAsync(createdReview);

            // Act
            var result = await _sut.CreateReviewAsync(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(PerformanceReviewStatus.Draft, result.Status);
            _reviewRepoMock.Verify(r => r.AddAsync(It.IsAny<PerformanceReview>()), Times.Once);
        }

        [Fact]
        public async Task CreateReviewAsync_WhenActiveReviewExists_ThrowsInvalidOperationException()
        {
            // Arrange
            var createDto = new CreatePerformanceReviewDto
            {
                EmployeeId = 1, ReviewerId = 2, Title = "Dup",
                CycleType = ReviewCycleType.Annual, ReviewYear = 2025, ReviewPeriodMonth = 1,
                ReviewStartDate = DateTime.Today, ReviewEndDate = DateTime.Today.AddYears(1),
                DueDate = DateTime.Today.AddMonths(2)
            };

            _employeeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(BuildEmployee(1));
            _employeeRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(BuildEmployee(2));
            _reviewRepoMock
                .Setup(r => r.HasActiveReviewAsync(1, 2025, ReviewCycleType.Annual))
                .ReturnsAsync(true); // Already exists

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateReviewAsync(createDto));
        }

        [Fact]
        public async Task SubmitSelfAssessmentAsync_UpdatesStatusToManagerReviewPending()
        {
            // Arrange
            var employee = BuildEmployee(1);
            var reviewer = BuildEmployee(2);
            var review = BuildReview(5, employee, reviewer, PerformanceReviewStatus.SelfAssessmentPending);
            var dto = new SelfAssessmentDto
            {
                ReviewId = 5,
                SelfRating = PerformanceRating.MeetsExpectations,
                SelfComments = "I met all my goals",
                GoalsAchieved = "Delivered project on time"
            };
            var updatedReview = BuildReview(5, employee, reviewer, PerformanceReviewStatus.ManagerReviewPending);
            updatedReview.SelfRating = PerformanceRating.MeetsExpectations;

            _reviewRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(review);
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(1);
            _reviewRepoMock.Setup(r => r.GetReviewWithDetailsAsync(5)).ReturnsAsync(updatedReview);

            // Act
            var result = await _sut.SubmitSelfAssessmentAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(PerformanceReviewStatus.ManagerReviewPending, result.Status);
            _unitOfWorkMock.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task FinalizeReviewAsync_UpdatesStatusToCompleted()
        {
            // Arrange
            var employee = BuildEmployee(1);
            var reviewer = BuildEmployee(2);
            var review = BuildReview(7, employee, reviewer, PerformanceReviewStatus.HrReviewPending);
            var finalizeDto = new FinalizeReviewDto
            {
                ReviewId = 7,
                OverallRating = PerformanceRating.ExceedsExpectations,
                OverallComments = "Great performance",
                HrComments = "Approved"
            };
            var completedReview = BuildReview(7, employee, reviewer, PerformanceReviewStatus.Completed);
            completedReview.OverallRating = PerformanceRating.ExceedsExpectations;

            _reviewRepoMock.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(review);
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(1);
            _reviewRepoMock.Setup(r => r.GetReviewWithDetailsAsync(7)).ReturnsAsync(completedReview);

            // Act
            var result = await _sut.FinalizeReviewAsync(finalizeDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(PerformanceReviewStatus.Completed, result.Status);
            Assert.Equal(PerformanceRating.ExceedsExpectations, result.OverallRating);
        }

        [Fact]
        public async Task AcknowledgeReviewAsync_WhenCompleted_ChangesStatusToAcknowledged()
        {
            // Arrange
            var employee = BuildEmployee(1);
            var reviewer = BuildEmployee(2);
            var review = BuildReview(8, employee, reviewer, PerformanceReviewStatus.Completed);
            var acknowledgedReview = BuildReview(8, employee, reviewer, PerformanceReviewStatus.Acknowledged);
            acknowledgedReview.AcknowledgedDate = FixedNow;

            _reviewRepoMock.Setup(r => r.GetByIdAsync(8)).ReturnsAsync(review);
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(1);
            _reviewRepoMock.Setup(r => r.GetReviewWithDetailsAsync(8)).ReturnsAsync(acknowledgedReview);

            // Act
            var result = await _sut.AcknowledgeReviewAsync(8, 1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(PerformanceReviewStatus.Acknowledged, result.Status);
            Assert.NotNull(result.AcknowledgedDate);
        }

        [Fact]
        public async Task AcknowledgeReviewAsync_WhenWrongEmployee_ThrowsInvalidOperationException()
        {
            // Arrange
            var employee = BuildEmployee(1);
            var reviewer = BuildEmployee(2);
            var review = BuildReview(9, employee, reviewer, PerformanceReviewStatus.Completed);

            _reviewRepoMock.Setup(r => r.GetByIdAsync(9)).ReturnsAsync(review);

            // Act & Assert - employee 99 trying to acknowledge employee 1's review
            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.AcknowledgeReviewAsync(9, 99));
        }

        [Fact]
        public async Task DeleteReviewAsync_OnlyDraftAllowed_DeletesSuccessfully()
        {
            // Arrange
            var employee = BuildEmployee(1);
            var reviewer = BuildEmployee(2);
            var review = BuildReview(3, employee, reviewer, PerformanceReviewStatus.Draft);

            _reviewRepoMock.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(review);
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            // Act
            var result = await _sut.DeleteReviewAsync(3);

            // Assert
            Assert.True(result);
            Assert.True(review.IsDeleted);
        }

        [Fact]
        public async Task DeleteReviewAsync_WhenNotDraft_ThrowsInvalidOperationException()
        {
            // Arrange
            var employee = BuildEmployee(1);
            var reviewer = BuildEmployee(2);
            var review = BuildReview(4, employee, reviewer, PerformanceReviewStatus.Completed);

            _reviewRepoMock.Setup(r => r.GetByIdAsync(4)).ReturnsAsync(review);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.DeleteReviewAsync(4));
        }

        [Fact]
        public async Task GetEmployeeReviewSummaryAsync_ReturnsCorrectSummary()
        {
            // Arrange
            var employee = BuildEmployee(1);
            var reviewer = BuildEmployee(2);

            var reviews = new List<PerformanceReview>
            {
                BuildReview(1, employee, reviewer, PerformanceReviewStatus.Completed),
                BuildReview(2, employee, reviewer, PerformanceReviewStatus.Acknowledged),
                BuildReview(3, employee, reviewer, PerformanceReviewStatus.ManagerReviewPending)
            };
            reviews[0].OverallRating = PerformanceRating.ExceedsExpectations;
            reviews[1].OverallRating = PerformanceRating.MeetsExpectations;

            _employeeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(employee);
            _reviewRepoMock.Setup(r => r.GetReviewsByEmployeeAsync(1)).ReturnsAsync(reviews);
            _reviewRepoMock.Setup(r => r.GetAverageRatingAsync(1)).ReturnsAsync(3.5);

            // Act
            var result = await _sut.GetEmployeeReviewSummaryAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.EmployeeId);
            Assert.Equal(3, result.TotalReviews);
            Assert.Equal(3.5, result.AverageRating);
            Assert.Equal(1, result.PendingReviews);
        }

        // ── Helpers ──────────────────────────────────────────────────────────────────

        private static Employee BuildEmployee(int id) => new()
        {
            Id = id,
            EmployeeCode = $"EMP{id:D5}",
            FirstName = "Test",
            LastName = "User",
            Email = $"test{id}@example.com",
            JobTitle = "Developer",
            DepartmentId = 1,
            Department = new Department { Id = 1, Code = "ENG", Name = "Engineering" }
        };

        private static PerformanceReview BuildReview(int id, Employee employee, Employee reviewer,
            PerformanceReviewStatus status = PerformanceReviewStatus.Draft) => new()
        {
            Id = id,
            EmployeeId = employee.Id,
            Employee = employee,
            ReviewerId = reviewer.Id,
            Reviewer = reviewer,
            Title = "Annual Review 2025",
            CycleType = ReviewCycleType.Annual,
            ReviewYear = 2025,
            ReviewPeriodMonth = 1,
            ReviewStartDate = new DateTime(2025, 1, 1),
            ReviewEndDate = new DateTime(2025, 12, 31),
            DueDate = new DateTime(2025, 2, 15),
            Status = status,
            CreatedAt = FixedNow
        };
    }
}
