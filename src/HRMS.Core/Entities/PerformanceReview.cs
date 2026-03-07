using HRMS.Core.Entities.Base;
using HRMS.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace HRMS.Core.Entities
{
    public class PerformanceReview : BaseEntity
    {
        public int EmployeeId { get; set; }

        public int ReviewerId { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        public ReviewCycleType CycleType { get; set; }

        public int ReviewYear { get; set; }

        public int ReviewPeriodMonth { get; set; }

        public DateTime ReviewStartDate { get; set; }

        public DateTime ReviewEndDate { get; set; }

        public DateTime DueDate { get; set; }

        public PerformanceReviewStatus Status { get; set; }

        // Self-assessment fields
        public PerformanceRating? SelfRating { get; set; }

        [StringLength(2000)]
        public string? SelfComments { get; set; }

        public DateTime? SelfAssessmentDate { get; set; }

        // Manager review fields
        public PerformanceRating? ManagerRating { get; set; }

        [StringLength(2000)]
        public string? ManagerComments { get; set; }

        public DateTime? ManagerReviewDate { get; set; }

        // HR review fields
        public PerformanceRating? HrRating { get; set; }

        [StringLength(2000)]
        public string? HrComments { get; set; }

        public DateTime? HrReviewDate { get; set; }

        // Overall / final
        public PerformanceRating? OverallRating { get; set; }

        [StringLength(2000)]
        public string? OverallComments { get; set; }

        // Scores (0-100)
        public double? TargetAchievementScore { get; set; }

        public double? CompetencyScore { get; set; }

        public double? OverallScore { get; set; }

        // Goals and development
        [StringLength(4000)]
        public string? GoalsSet { get; set; }

        [StringLength(4000)]
        public string? GoalsAchieved { get; set; }

        [StringLength(2000)]
        public string? DevelopmentPlan { get; set; }

        // Acknowledgement
        public DateTime? AcknowledgedDate { get; set; }

        // Navigation properties
        public virtual Employee Employee { get; set; } = null!;
        public virtual Employee Reviewer { get; set; } = null!;
    }
}
