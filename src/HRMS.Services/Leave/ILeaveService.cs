using HRMS.Core.Enums;
using HRMS.Services.Leave.Dtos;

namespace HRMS.Services.Leave
{
    /// <summary>
    /// Service interface for managing leave requests, balances, and approvals.
    /// </summary>
    public interface ILeaveService
    {
        // Get operations
        /// <summary>
        /// Retrieves a leave request by its unique identifier.
        /// </summary>
        /// <param name="id">The leave request ID.</param>
        /// <returns>The leave request details or null if not found.</returns>
        Task<LeaveRequestDto?> GetLeaveRequestByIdAsync(int id);

        /// <summary>
        /// Retrieves all leave requests in the system.
        /// </summary>
        /// <returns>A collection of all leave requests.</returns>
        Task<IEnumerable<LeaveRequestDto>> GetAllLeaveRequestsAsync();

        /// <summary>
        /// Retrieves all leave requests for a specific employee.
        /// </summary>
        /// <param name="employeeId">The employee ID.</param>
        /// <returns>A collection of leave requests for the employee.</returns>
        Task<IEnumerable<LeaveRequestDto>> GetLeaveRequestsByEmployeeAsync(int employeeId);

        /// <summary>
        /// Retrieves leave requests filtered by status.
        /// </summary>
        /// <param name="status">The leave request status to filter by.</param>
        /// <returns>A collection of leave requests with the specified status.</returns>
        Task<IEnumerable<LeaveRequestDto>> GetLeaveRequestsByStatusAsync(LeaveStatus status);

        /// <summary>
        /// Retrieves leave requests within a specific date range.
        /// </summary>
        /// <param name="startDate">The start date of the range.</param>
        /// <param name="endDate">The end date of the range.</param>
        /// <returns>A collection of leave requests within the date range.</returns>
        Task<IEnumerable<LeaveRequestDto>> GetLeaveRequestsByDateRangeAsync(DateTime startDate, DateTime endDate);

        // Create/Update/Delete
        /// <summary>
        /// Creates a new leave request.
        /// </summary>
        /// <param name="createDto">The leave request data to create.</param>
        /// <returns>The created leave request details.</returns>
        Task<LeaveRequestDto> CreateLeaveRequestAsync(CreateLeaveRequestDto createDto);

        /// <summary>
        /// Updates an existing leave request.
        /// </summary>
        /// <param name="updateDto">The leave request data to update.</param>
        /// <returns>The updated leave request details.</returns>
        Task<LeaveRequestDto> UpdateLeaveRequestAsync(UpdateLeaveRequestDto updateDto);

        /// <summary>
        /// Approves a leave request.
        /// </summary>
        /// <param name="approveDto">The approval data including optional comments.</param>
        /// <param name="approverId">The ID of the approver (manager/HR).</param>
        /// <returns>The approved leave request details.</returns>
        Task<LeaveRequestDto> ApproveLeaveRequestAsync(ApproveLeaveDto approveDto, int approverId);

        /// <summary>
        /// Rejects a leave request.
        /// </summary>
        /// <param name="rejectDto">The rejection data including reason.</param>
        /// <param name="approverId">The ID of the approver (manager/HR).</param>
        /// <returns>The rejected leave request details.</returns>
        Task<LeaveRequestDto> RejectLeaveRequestAsync(RejectLeaveDto rejectDto, int approverId);

        /// <summary>
        /// Cancels a leave request by the employee.
        /// </summary>
        /// <param name="id">The leave request ID to cancel.</param>
        /// <param name="employeeId">The employee ID who is canceling the request.</param>
        /// <returns>True if cancellation was successful, false otherwise.</returns>
        Task<bool> CancelLeaveRequestAsync(int id, int employeeId);

        /// <summary>
        /// Deletes a leave request.
        /// </summary>
        /// <param name="id">The leave request ID to delete.</param>
        /// <returns>True if deletion was successful, false otherwise.</returns>
        Task<bool> DeleteLeaveRequestAsync(int id);

        // Balance and validation
        /// <summary>
        /// Retrieves the leave balance for an employee for a specific year.
        /// </summary>
        /// <param name="employeeId">The employee ID.</param>
        /// <param name="year">The year to check the balance for.</param>
        /// <returns>The leave balance details including available days for each leave type.</returns>
        Task<LeaveBalanceDto> GetLeaveBalanceAsync(int employeeId, int year);

        /// <summary>
        /// Gets the number of available leave days for a specific leave type.
        /// </summary>
        /// <param name="employeeId">The employee ID.</param>
        /// <param name="year">The year to check.</param>
        /// <param name="leaveType">The type of leave.</param>
        /// <returns>The number of available leave days.</returns>
        Task<int> GetAvailableLeaveDaysAsync(int employeeId, int year, LeaveType leaveType);

        /// <summary>
        /// Checks if an employee can apply for leave based on available balance.
        /// </summary>
        /// <param name="employeeId">The employee ID.</param>
        /// <param name="leaveType">The type of leave.</param>
        /// <param name="startDate">The start date of the leave.</param>
        /// <param name="endDate">The end date of the leave.</param>
        /// <returns>True if the employee can apply for leave, false otherwise.</returns>
        Task<bool> CanApplyLeaveAsync(int employeeId, LeaveType leaveType, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Checks if an employee has overlapping leave requests.
        /// </summary>
        /// <param name="employeeId">The employee ID.</param>
        /// <param name="startDate">The start date to check.</param>
        /// <param name="endDate">The end date to check.</param>
        /// <param name="excludeId">Optional leave request ID to exclude from the check (for updates).</param>
        /// <returns>True if there are overlapping leave requests, false otherwise.</returns>
        Task<bool> HasOverlappingLeaveAsync(int employeeId, DateTime startDate, DateTime endDate, int? excludeId = null);

        // Manager/Admin operations
        /// <summary>
        /// Retrieves leave requests pending approval for a specific manager.
        /// </summary>
        /// <param name="managerId">The manager's employee ID.</param>
        /// <returns>A collection of pending leave requests for the manager's team.</returns>
        Task<IEnumerable<LeaveRequestDto>> GetPendingApprovalsAsync(int managerId);

        /// <summary>
        /// Retrieves the leave calendar for a manager's team for a specific month.
        /// </summary>
        /// <param name="managerId">The manager's employee ID.</param>
        /// <param name="month">The month to retrieve the calendar for.</param>
        /// <returns>A collection of leave requests for the team during the month.</returns>
        Task<IEnumerable<LeaveRequestDto>> GetTeamLeaveCalendarAsync(int managerId, DateTime month);

        /// <summary>
        /// Gets leave statistics grouped by status within a date range.
        /// </summary>
        /// <param name="startDate">The start date of the range.</param>
        /// <param name="endDate">The end date of the range.</param>
        /// <returns>A dictionary with leave status counts.</returns>
        Task<Dictionary<LeaveStatus, int>> GetLeaveStatisticsAsync(DateTime startDate, DateTime endDate);

        // Dashboard
        /// <summary>
        /// Gets the count of employees currently on leave on a specific date.
        /// </summary>
        /// <param name="date">The date to check.</param>
        /// <returns>The number of employees on leave.</returns>
        Task<int> GetEmployeesOnLeaveAsync(DateTime date);

        /// <summary>
        /// Retrieves upcoming leave requests within the specified number of days.
        /// </summary>
        /// <param name="days">The number of days to look ahead.</param>
        /// <returns>A collection of upcoming leave requests.</returns>
        Task<IEnumerable<LeaveRequestDto>> GetUpcomingLeavesAsync(int days);
    }
}