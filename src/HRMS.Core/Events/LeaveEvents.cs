using HRMS.Core.Enums;

namespace HRMS.Core.Events
{
    /// <summary>Raised when a new leave request is submitted by an employee.</summary>
    public sealed class LeaveRequestCreatedEvent : DomainEventBase
    {
        public int LeaveRequestId { get; }
        public int EmployeeId { get; }
        public LeaveType LeaveType { get; }
        public DateTime StartDate { get; }
        public DateTime EndDate { get; }
        public decimal TotalDays { get; }

        public LeaveRequestCreatedEvent(
            int leaveRequestId, int employeeId, LeaveType leaveType,
            DateTime startDate, DateTime endDate, decimal totalDays)
        {
            LeaveRequestId = leaveRequestId;
            EmployeeId = employeeId;
            LeaveType = leaveType;
            StartDate = startDate;
            EndDate = endDate;
            TotalDays = totalDays;
        }
    }

    /// <summary>Raised when a leave request is approved by a manager.</summary>
    public sealed class LeaveRequestApprovedEvent : DomainEventBase
    {
        public int LeaveRequestId { get; }
        public int EmployeeId { get; }
        public int ApproverId { get; }
        public string? Remarks { get; }

        public LeaveRequestApprovedEvent(int leaveRequestId, int employeeId, int approverId, string? remarks)
        {
            LeaveRequestId = leaveRequestId;
            EmployeeId = employeeId;
            ApproverId = approverId;
            Remarks = remarks;
        }
    }

    /// <summary>Raised when a leave request is rejected by a manager.</summary>
    public sealed class LeaveRequestRejectedEvent : DomainEventBase
    {
        public int LeaveRequestId { get; }
        public int EmployeeId { get; }
        public int RejectedById { get; }
        public string? Remarks { get; }

        public LeaveRequestRejectedEvent(int leaveRequestId, int employeeId, int rejectedById, string? remarks)
        {
            LeaveRequestId = leaveRequestId;
            EmployeeId = employeeId;
            RejectedById = rejectedById;
            Remarks = remarks;
        }
    }
}
