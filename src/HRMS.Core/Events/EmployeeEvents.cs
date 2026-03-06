using HRMS.Core.Enums;

namespace HRMS.Core.Events
{
    /// <summary>Raised when a new employee is created in the system.</summary>
    public sealed class EmployeeCreatedEvent : DomainEventBase
    {
        public int EmployeeId { get; }
        public string EmployeeCode { get; }
        public string FullName { get; }
        public int DepartmentId { get; }

        public EmployeeCreatedEvent(int employeeId, string employeeCode, string fullName, int departmentId)
        {
            EmployeeId = employeeId;
            EmployeeCode = employeeCode;
            FullName = fullName;
            DepartmentId = departmentId;
        }
    }

    /// <summary>Raised when an employee's status changes (e.g., activated, deactivated, terminated).</summary>
    public sealed class EmployeeStatusChangedEvent : DomainEventBase
    {
        public int EmployeeId { get; }
        public EmployeeStatus OldStatus { get; }
        public EmployeeStatus NewStatus { get; }

        public EmployeeStatusChangedEvent(int employeeId, EmployeeStatus oldStatus, EmployeeStatus newStatus)
        {
            EmployeeId = employeeId;
            OldStatus = oldStatus;
            NewStatus = newStatus;
        }
    }

    /// <summary>Raised when an employee's department assignment changes.</summary>
    public sealed class EmployeeDepartmentChangedEvent : DomainEventBase
    {
        public int EmployeeId { get; }
        public int OldDepartmentId { get; }
        public int NewDepartmentId { get; }

        public EmployeeDepartmentChangedEvent(int employeeId, int oldDepartmentId, int newDepartmentId)
        {
            EmployeeId = employeeId;
            OldDepartmentId = oldDepartmentId;
            NewDepartmentId = newDepartmentId;
        }
    }
}
