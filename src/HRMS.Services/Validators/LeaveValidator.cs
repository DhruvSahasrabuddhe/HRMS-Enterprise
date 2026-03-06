using FluentValidation;
using HRMS.Core.Enums;
using HRMS.Services.Leave.Dtos;

namespace HRMS.Services.Validators
{
    /// <summary>
    /// Validator for creating leave requests.
    /// </summary>
    public class CreateLeaveRequestValidator : AbstractValidator<CreateLeaveRequestDto>
    {
        public CreateLeaveRequestValidator()
        {
            RuleFor(x => x.EmployeeId)
                .GreaterThan(0).WithMessage("Valid employee ID is required");

            RuleFor(x => x.LeaveType)
                .IsInEnum().WithMessage("Valid leave type is required");

            RuleFor(x => x.StartDate)
                .NotEmpty().WithMessage("Start date is required")
                .GreaterThanOrEqualTo(DateTime.Today).WithMessage("Start date cannot be in the past");

            RuleFor(x => x.EndDate)
                .NotEmpty().WithMessage("End date is required")
                .GreaterThanOrEqualTo(x => x.StartDate).WithMessage("End date must be after or equal to start date");

            RuleFor(x => x.Reason)
                .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters")
                .NotEmpty().When(x => x.LeaveType == LeaveType.Sick)
                .WithMessage("Reason is required for sick leave");
        }
    }

    /// <summary>
    /// Validator for approving leave requests.
    /// </summary>
    public class ApproveLeaveValidator : AbstractValidator<ApproveLeaveDto>
    {
        public ApproveLeaveValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Valid leave request ID is required");

            RuleFor(x => x.Remarks)
                .MaximumLength(500).WithMessage("Remarks cannot exceed 500 characters");
        }
    }

    /// <summary>
    /// Validator for rejecting leave requests.
    /// </summary>
    public class RejectLeaveValidator : AbstractValidator<RejectLeaveDto>
    {
        public RejectLeaveValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Valid leave request ID is required");

            RuleFor(x => x.Remarks)
                .NotEmpty().WithMessage("Remarks are required when rejecting a leave request")
                .MaximumLength(500).WithMessage("Remarks cannot exceed 500 characters");
        }
    }
}