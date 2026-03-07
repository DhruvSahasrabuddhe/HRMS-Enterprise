using FluentValidation;
using HRMS.Services.Departments.Dtos;
using HRMS.Shared.Constants;
using System.Text.RegularExpressions;

namespace HRMS.Services.Validators
{
    /// <summary>
    /// Validator for creating new department records.
    /// </summary>
    public class CreateDepartmentValidator : AbstractValidator<CreateDepartmentDto>
    {
        // Regex to detect potentially dangerous HTML/script content
        private static readonly Regex HtmlScriptPattern = new(@"<script|<iframe|javascript:|onerror=|onclick=", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public CreateDepartmentValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Department code is required")
                .MaximumLength(20).WithMessage("Department code cannot exceed 20 characters")
                .Matches(@"^[A-Z0-9]+$").WithMessage("Department code must contain only uppercase letters and numbers");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Department name is required")
                .MaximumLength(100).WithMessage("Department name cannot exceed 100 characters")
                .Must(NotContainScriptTags).WithMessage("Department name contains potentially dangerous content");

            RuleFor(x => x.Description)
                .MaximumLength(500).When(x => !string.IsNullOrEmpty(x.Description))
                .WithMessage("Description cannot exceed 500 characters")
                .Must(NotContainScriptTags).When(x => !string.IsNullOrEmpty(x.Description))
                .WithMessage("Description contains potentially dangerous content");

            RuleFor(x => x.Email)
                .EmailAddress().When(x => !string.IsNullOrEmpty(x.Email))
                .WithMessage("Invalid email format")
                .MaximumLength(100).When(x => !string.IsNullOrEmpty(x.Email))
                .WithMessage("Email cannot exceed 100 characters");

            RuleFor(x => x.Phone)
                .MaximumLength(20).WithMessage("Phone number cannot exceed 20 characters")
                .Matches(@"^[0-9+\-\s()]*$").When(x => !string.IsNullOrEmpty(x.Phone))
                .WithMessage("Invalid phone number format");

            RuleFor(x => x.Budget)
                .GreaterThanOrEqualTo(0).When(x => x.Budget.HasValue)
                .WithMessage("Budget must be a positive number")
                .LessThan(HrmsConstants.Validation.MaxRealisticBudget).When(x => x.Budget.HasValue)
                .WithMessage("Budget value seems unrealistic");
        }

        private bool NotContainScriptTags(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return true;

            return !HtmlScriptPattern.IsMatch(value);
        }
    }

    /// <summary>
    /// Validator for updating existing department records.
    /// </summary>
    public class UpdateDepartmentValidator : AbstractValidator<UpdateDepartmentDto>
    {
        // Regex to detect potentially dangerous HTML/script content
        private static readonly Regex HtmlScriptPattern = new(@"<script|<iframe|javascript:|onerror=|onclick=", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public UpdateDepartmentValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Valid department ID is required");

            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Department code is required")
                .MaximumLength(20).WithMessage("Department code cannot exceed 20 characters");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Department name is required")
                .MaximumLength(100).WithMessage("Department name cannot exceed 100 characters")
                .Must(NotContainScriptTags).WithMessage("Department name contains potentially dangerous content");
        }

        private bool NotContainScriptTags(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return true;

            return !HtmlScriptPattern.IsMatch(value);
        }
    }
}