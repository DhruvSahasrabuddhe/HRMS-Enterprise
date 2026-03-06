using FluentValidation;
using HRMS.Services.Employees.Dtos;
using System.Text.RegularExpressions;

namespace HRMS.Services.Validators
{
    /// <summary>
    /// Validator for creating new employee records.
    /// </summary>
    public class CreateEmployeeValidator : AbstractValidator<CreateEmployeeDto>
    {
        // Regex to detect potentially dangerous HTML/script content
        private static readonly Regex HtmlScriptPattern = new(@"<script|<iframe|javascript:|onerror=|onclick=", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public CreateEmployeeValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required")
                .MaximumLength(50).WithMessage("First name cannot exceed 50 characters")
                .Must(BeValidName).WithMessage("First name contains invalid characters")
                .Must(NotContainScriptTags).WithMessage("First name contains potentially dangerous content");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required")
                .MaximumLength(50).WithMessage("Last name cannot exceed 50 characters")
                .Must(BeValidName).WithMessage("Last name contains invalid characters")
                .Must(NotContainScriptTags).WithMessage("Last name contains potentially dangerous content");

            RuleFor(x => x.MiddleName)
                .MaximumLength(50).WithMessage("Middle name cannot exceed 50 characters")
                .Must(NotContainScriptTags).When(x => !string.IsNullOrEmpty(x.MiddleName))
                .WithMessage("Middle name contains potentially dangerous content");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format")
                .MaximumLength(100).WithMessage("Email cannot exceed 100 characters");

            RuleFor(x => x.Phone)
                .MaximumLength(20).WithMessage("Phone number cannot exceed 20 characters")
                .Matches(@"^[0-9+\-\s()]*$").When(x => !string.IsNullOrEmpty(x.Phone))
                .WithMessage("Invalid phone number format");

            RuleFor(x => x.Mobile)
                .MaximumLength(20).WithMessage("Mobile number cannot exceed 20 characters")
                .Matches(@"^[0-9+\-\s()]*$").When(x => !string.IsNullOrEmpty(x.Mobile))
                .WithMessage("Invalid mobile number format");

            RuleFor(x => x.DateOfBirth)
                .NotEmpty().WithMessage("Date of birth is required")
                .LessThan(DateTime.Today).WithMessage("Date of birth must be in the past")
                .GreaterThan(DateTime.Today.AddYears(-100)).WithMessage("Invalid date of birth")
                .Must(BeAtLeast18YearsOld).WithMessage("Employee must be at least 18 years old");

            RuleFor(x => x.HireDate)
                .NotEmpty().WithMessage("Hire date is required")
                .LessThanOrEqualTo(DateTime.Today).WithMessage("Hire date cannot be in the future");

            RuleFor(x => x.Salary)
                .GreaterThanOrEqualTo(0).WithMessage("Salary must be a positive number")
                .LessThan(10000000).WithMessage("Salary value seems unrealistic");

            RuleFor(x => x.DepartmentId)
                .GreaterThan(0).WithMessage("Valid department is required");

            RuleFor(x => x.JobTitle)
                .NotEmpty().WithMessage("Job title is required")
                .MaximumLength(100).WithMessage("Job title cannot exceed 100 characters")
                .Must(NotContainScriptTags).WithMessage("Job title contains potentially dangerous content");

            RuleFor(x => x.NationalId)
                .MaximumLength(20).WithMessage("National ID cannot exceed 20 characters")
                .Matches(@"^[A-Za-z0-9\-]*$").When(x => !string.IsNullOrEmpty(x.NationalId))
                .WithMessage("National ID can only contain letters, numbers, and hyphens");

            RuleFor(x => x.PassportNumber)
                .MaximumLength(20).WithMessage("Passport number cannot exceed 20 characters")
                .Matches(@"^[A-Za-z0-9\-]*$").When(x => !string.IsNullOrEmpty(x.PassportNumber))
                .WithMessage("Passport number can only contain letters, numbers, and hyphens");

            RuleFor(x => x.Address)
                .MaximumLength(500).WithMessage("Address cannot exceed 500 characters")
                .Must(NotContainScriptTags).When(x => !string.IsNullOrEmpty(x.Address))
                .WithMessage("Address contains potentially dangerous content");

            RuleFor(x => x.City)
                .MaximumLength(100).WithMessage("City cannot exceed 100 characters")
                .Must(NotContainScriptTags).When(x => !string.IsNullOrEmpty(x.City))
                .WithMessage("City contains potentially dangerous content");

            RuleFor(x => x.BankAccount)
                .MaximumLength(50).WithMessage("Bank account cannot exceed 50 characters")
                .Matches(@"^[A-Za-z0-9\-]*$").When(x => !string.IsNullOrEmpty(x.BankAccount))
                .WithMessage("Bank account can only contain letters, numbers, and hyphens");
        }

        private bool BeValidName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            // Names should only contain letters, spaces, hyphens, and apostrophes
            return Regex.IsMatch(name, @"^[a-zA-Z\s\-']+$");
        }

        private bool NotContainScriptTags(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return true;

            return !HtmlScriptPattern.IsMatch(value);
        }

        private bool BeAtLeast18YearsOld(DateTime dateOfBirth)
        {
            var age = DateTime.Today.Year - dateOfBirth.Year;
            if (dateOfBirth.Date > DateTime.Today.AddYears(-age))
                age--;

            return age >= 18;
        }
    }

    /// <summary>
    /// Validator for updating existing employee records.
    /// </summary>
    public class UpdateEmployeeValidator : AbstractValidator<UpdateEmployeeDto>
    {
        // Regex to detect potentially dangerous HTML/script content
        private static readonly Regex HtmlScriptPattern = new(@"<script|<iframe|javascript:|onerror=|onclick=", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public UpdateEmployeeValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Valid employee ID is required");

            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required")
                .MaximumLength(50).WithMessage("First name cannot exceed 50 characters")
                .Must(BeValidName).WithMessage("First name contains invalid characters")
                .Must(NotContainScriptTags).WithMessage("First name contains potentially dangerous content");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required")
                .MaximumLength(50).WithMessage("Last name cannot exceed 50 characters")
                .Must(BeValidName).WithMessage("Last name contains invalid characters")
                .Must(NotContainScriptTags).WithMessage("Last name contains potentially dangerous content");

            RuleFor(x => x.MiddleName)
                .MaximumLength(50).WithMessage("Middle name cannot exceed 50 characters")
                .Must(NotContainScriptTags).When(x => !string.IsNullOrEmpty(x.MiddleName))
                .WithMessage("Middle name contains potentially dangerous content");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format")
                .MaximumLength(100).WithMessage("Email cannot exceed 100 characters");

            RuleFor(x => x.Phone)
                .MaximumLength(20).WithMessage("Phone number cannot exceed 20 characters")
                .Matches(@"^[0-9+\-\s()]*$").When(x => !string.IsNullOrEmpty(x.Phone))
                .WithMessage("Invalid phone number format");

            RuleFor(x => x.Mobile)
                .MaximumLength(20).WithMessage("Mobile number cannot exceed 20 characters")
                .Matches(@"^[0-9+\-\s()]*$").When(x => !string.IsNullOrEmpty(x.Mobile))
                .WithMessage("Invalid mobile number format");

            RuleFor(x => x.Salary)
                .GreaterThanOrEqualTo(0).WithMessage("Salary must be a positive number")
                .LessThan(10000000).WithMessage("Salary value seems unrealistic");

            RuleFor(x => x.DepartmentId)
                .GreaterThan(0).WithMessage("Valid department is required");

            RuleFor(x => x.JobTitle)
                .NotEmpty().WithMessage("Job title is required")
                .MaximumLength(100).WithMessage("Job title cannot exceed 100 characters")
                .Must(NotContainScriptTags).WithMessage("Job title contains potentially dangerous content");

            RuleFor(x => x.Address)
                .MaximumLength(500).WithMessage("Address cannot exceed 500 characters")
                .Must(NotContainScriptTags).When(x => !string.IsNullOrEmpty(x.Address))
                .WithMessage("Address contains potentially dangerous content");

            RuleFor(x => x.City)
                .MaximumLength(100).WithMessage("City cannot exceed 100 characters")
                .Must(NotContainScriptTags).When(x => !string.IsNullOrEmpty(x.City))
                .WithMessage("City contains potentially dangerous content");

            RuleFor(x => x.BankAccount)
                .MaximumLength(50).WithMessage("Bank account cannot exceed 50 characters")
                .Matches(@"^[A-Za-z0-9\-]*$").When(x => !string.IsNullOrEmpty(x.BankAccount))
                .WithMessage("Bank account can only contain letters, numbers, and hyphens");
        }

        private bool BeValidName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            // Names should only contain letters, spaces, hyphens, and apostrophes
            return Regex.IsMatch(name, @"^[a-zA-Z\s\-']+$");
        }

        private bool NotContainScriptTags(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return true;

            return !HtmlScriptPattern.IsMatch(value);
        }
    }
}