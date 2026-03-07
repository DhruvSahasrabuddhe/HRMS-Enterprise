using HRMS.Core.Enums;

namespace HRMS.Services.PerformanceReviews.Dtos
{
    public class PerformanceReviewDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string EmployeeCode { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public int ReviewerId { get; set; }
        public string ReviewerName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public ReviewCycleType CycleType { get; set; }
        public int ReviewYear { get; set; }
        public int ReviewPeriodMonth { get; set; }
        public DateTime ReviewStartDate { get; set; }
        public DateTime ReviewEndDate { get; set; }
        public DateTime DueDate { get; set; }
        public PerformanceReviewStatus Status { get; set; }
        public PerformanceRating? SelfRating { get; set; }
        public string? SelfComments { get; set; }
        public DateTime? SelfAssessmentDate { get; set; }
        public PerformanceRating? ManagerRating { get; set; }
        public string? ManagerComments { get; set; }
        public DateTime? ManagerReviewDate { get; set; }
        public PerformanceRating? HrRating { get; set; }
        public string? HrComments { get; set; }
        public DateTime? HrReviewDate { get; set; }
        public PerformanceRating? OverallRating { get; set; }
        public string? OverallComments { get; set; }
        public double? TargetAchievementScore { get; set; }
        public double? CompetencyScore { get; set; }
        public double? OverallScore { get; set; }
        public string? GoalsSet { get; set; }
        public string? GoalsAchieved { get; set; }
        public string? DevelopmentPlan { get; set; }
        public DateTime? AcknowledgedDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreatePerformanceReviewDto
    {
        public int EmployeeId { get; set; }
        public int ReviewerId { get; set; }
        public string Title { get; set; } = string.Empty;
        public ReviewCycleType CycleType { get; set; }
        public int ReviewYear { get; set; }
        public int ReviewPeriodMonth { get; set; }
        public DateTime ReviewStartDate { get; set; }
        public DateTime ReviewEndDate { get; set; }
        public DateTime DueDate { get; set; }
        public string? GoalsSet { get; set; }
    }

    public class UpdatePerformanceReviewDto
    {
        public int Id { get; set; }
        public string? GoalsSet { get; set; }
        public string? DevelopmentPlan { get; set; }
        public DateTime? DueDate { get; set; }
    }

    public class SelfAssessmentDto
    {
        public int ReviewId { get; set; }
        public PerformanceRating SelfRating { get; set; }
        public string? SelfComments { get; set; }
        public string? GoalsAchieved { get; set; }
    }

    public class ManagerReviewDto
    {
        public int ReviewId { get; set; }
        public PerformanceRating ManagerRating { get; set; }
        public string? ManagerComments { get; set; }
        public double? TargetAchievementScore { get; set; }
        public double? CompetencyScore { get; set; }
        public string? DevelopmentPlan { get; set; }
    }

    public class FinalizeReviewDto
    {
        public int ReviewId { get; set; }
        public PerformanceRating OverallRating { get; set; }
        public string? OverallComments { get; set; }
        public string? HrComments { get; set; }
    }

    public class ReviewSummaryDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public int TotalReviews { get; set; }
        public double? AverageRating { get; set; }
        public PerformanceRating? LatestRating { get; set; }
        public int PendingReviews { get; set; }
    }
}
