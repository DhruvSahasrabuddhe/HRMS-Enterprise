using HRMS.Core.CQRS;
using HRMS.Core.Enums;
using HRMS.Services.Employees.Dtos;

namespace HRMS.Services.Employees.Commands
{
    /// <summary>Command to create a new employee record.</summary>
    public sealed class CreateEmployeeCommand : ICommand<EmployeeDto>
    {
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string? MiddleName { get; init; }
        public string Email { get; init; } = string.Empty;
        public string? Phone { get; init; }
        public string? Mobile { get; init; }
        public DateTime DateOfBirth { get; init; }
        public Gender Gender { get; init; }
        public MaritalStatus MaritalStatus { get; init; }
        public DateTime HireDate { get; init; }
        public EmploymentType EmploymentType { get; init; }
        public string JobTitle { get; init; } = string.Empty;
        public string? JobGrade { get; init; }
        public decimal Salary { get; init; }
        public int DepartmentId { get; init; }
        public int? ManagerId { get; init; }
        public string? Address { get; init; }
        public string? City { get; init; }
        public string? State { get; init; }
        public string? Country { get; init; }
        public string? PostalCode { get; init; }
    }

    /// <summary>Command to update an existing employee record.</summary>
    public sealed class UpdateEmployeeCommand : ICommand<EmployeeDto>
    {
        public int Id { get; init; }
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string? MiddleName { get; init; }
        public string Email { get; init; } = string.Empty;
        public string? Phone { get; init; }
        public string? Mobile { get; init; }
        public string JobTitle { get; init; } = string.Empty;
        public string? JobGrade { get; init; }
        public decimal Salary { get; init; }
        public int DepartmentId { get; init; }
        public int? ManagerId { get; init; }
        public EmployeeStatus Status { get; init; }
        public string? Address { get; init; }
        public string? City { get; init; }
        public string? State { get; init; }
        public string? Country { get; init; }
    }

    /// <summary>Command to soft-delete an employee.</summary>
    public sealed class DeleteEmployeeCommand : ICommand
    {
        public int EmployeeId { get; init; }

        public DeleteEmployeeCommand(int employeeId)
        {
            EmployeeId = employeeId;
        }
    }
}
