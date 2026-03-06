using FluentValidation;
using HRMS.Services.Employees.Dtos;

namespace HRMS.Services.Validators
{
    /// <summary>
    /// Validator for creating new employee records.
    /// </summary>
    public class CreateEmployeeValidator : AbstractValidator<CreateEmployeeDto>
    {
        public CreateEmployeeValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required")
                .MaximumLength(50).WithMessage("First name cannot exceed 50 characters");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required")
                .MaximumLength(50).WithMessage("Last name cannot exceed 50 characters");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format")
                .MaximumLength(100).WithMessage("Email cannot exceed 100 characters");

            RuleFor(x => x.Phone)
                .MaximumLength(20).WithMessage("Phone number cannot exceed 20 characters")
                .Matches(@"^[0-9+\-\s()]*$").When(x => !string.IsNullOrEmpty(x.Phone))
                .WithMessage("Invalid phone number format");

            RuleFor(x => x.DateOfBirth)
                .NotEmpty().WithMessage("Date of birth is required")
                .LessThan(DateTime.Today).WithMessage("Date of birth must be in the past")
                .GreaterThan(DateTime.Today.AddYears(-100)).WithMessage("Invalid date of birth");

            RuleFor(x => x.HireDate)
                .NotEmpty().WithMessage("Hire date is required")
                .LessThanOrEqualTo(DateTime.Today).WithMessage("Hire date cannot be in the future");

            RuleFor(x => x.Salary)
                .GreaterThanOrEqualTo(0).WithMessage("Salary must be a positive number");

            RuleFor(x => x.DepartmentId)
                .GreaterThan(0).WithMessage("Valid department is required");

            RuleFor(x => x.JobTitle)
                .NotEmpty().WithMessage("Job title is required")
                .MaximumLength(100).WithMessage("Job title cannot exceed 100 characters");
        }
    }

    /// <summary>
    /// Validator for updating existing employee records.
    /// </summary>
    public class UpdateEmployeeValidator : AbstractValidator<UpdateEmployeeDto>
    {
        public UpdateEmployeeValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Valid employee ID is required");

            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required")
                .MaximumLength(50).WithMessage("First name cannot exceed 50 characters");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required")
                .MaximumLength(50).WithMessage("Last name cannot exceed 50 characters");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format")
                .MaximumLength(100).WithMessage("Email cannot exceed 100 characters");

            RuleFor(x => x.Salary)
                .GreaterThanOrEqualTo(0).WithMessage("Salary must be a positive number");

            RuleFor(x => x.DepartmentId)
                .GreaterThan(0).WithMessage("Valid department is required");

            RuleFor(x => x.JobTitle)
                .NotEmpty().WithMessage("Job title is required")
                .MaximumLength(100).WithMessage("Job title cannot exceed 100 characters");
        }
    }
}