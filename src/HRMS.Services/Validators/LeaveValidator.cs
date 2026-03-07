using FluentValidation;
using HRMS.Core.Enums;
using HRMS.Services.Leave.Dtos;
using System.Text.RegularExpressions;

namespace HRMS.Services.Validators
{
    /// <summary>
    /// Validator for creating leave requests.
    /// </summary>
    public class CreateLeaveRequestValidator : AbstractValidator<CreateLeaveRequestDto>
    {
        // Regex to detect potentially dangerous HTML/script content
        private static readonly Regex HtmlScriptPattern = new(@"<script|<iframe|javascript:|onerror=|onclick=", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public CreateLeaveRequestValidator()
        {
            RuleFor(x => x.EmployeeId)
                .GreaterThan(0).WithMessage("Valid employee ID is required");

            RuleFor(x => x.LeaveType)
                .IsInEnum().WithMessage("Valid leave type is required");

            RuleFor(x => x.StartDate)
                .NotEmpty().WithMessage("Start date is required")
                .GreaterThanOrEqualTo(DateTime.Today).WithMessage("Start date cannot be in the past")
                .LessThan(DateTime.Today.AddYears(1)).WithMessage("Leave start date cannot be more than 1 year in the future");

            RuleFor(x => x.EndDate)
                .NotEmpty().WithMessage("End date is required")
                .GreaterThanOrEqualTo(x => x.StartDate).WithMessage("End date must be after or equal to start date")
                .LessThan(x => x.StartDate.AddMonths(3)).WithMessage("Leave duration cannot exceed 3 months");

            RuleFor(x => x.Reason)
                .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters")
                .NotEmpty().When(x => x.LeaveType == LeaveType.Sick)
                .WithMessage("Reason is required for sick leave")
                .Must(NotContainScriptTags).When(x => !string.IsNullOrEmpty(x.Reason))
                .WithMessage("Reason contains potentially dangerous content");
        }

        private bool NotContainScriptTags(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return true;

            return !HtmlScriptPattern.IsMatch(value);
        }
    }

    /// <summary>
    /// Validator for approving leave requests.
    /// </summary>
    public class ApproveLeaveValidator : AbstractValidator<ApproveLeaveDto>
    {
        // Regex to detect potentially dangerous HTML/script content
        private static readonly Regex HtmlScriptPattern = new(@"<script|<iframe|javascript:|onerror=|onclick=", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public ApproveLeaveValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Valid leave request ID is required");

            RuleFor(x => x.Remarks)
                .MaximumLength(500).WithMessage("Remarks cannot exceed 500 characters")
                .Must(NotContainScriptTags).When(x => !string.IsNullOrEmpty(x.Remarks))
                .WithMessage("Remarks contain potentially dangerous content");
        }

        private bool NotContainScriptTags(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return true;

            return !HtmlScriptPattern.IsMatch(value);
        }
    }

    /// <summary>
    /// Validator for rejecting leave requests.
    /// </summary>
    public class RejectLeaveValidator : AbstractValidator<RejectLeaveDto>
    {
        // Regex to detect potentially dangerous HTML/script content
        private static readonly Regex HtmlScriptPattern = new(@"<script|<iframe|javascript:|onerror=|onclick=", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public RejectLeaveValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Valid leave request ID is required");

            RuleFor(x => x.Remarks)
                .NotEmpty().WithMessage("Remarks are required when rejecting a leave request")
                .MaximumLength(500).WithMessage("Remarks cannot exceed 500 characters")
                .Must(NotContainScriptTags).WithMessage("Remarks contain potentially dangerous content");
        }

        private bool NotContainScriptTags(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return true;

            return !HtmlScriptPattern.IsMatch(value);
        }
    }
}