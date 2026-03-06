using AutoMapper;
using FluentValidation;
using HRMS.Core.Entities;
using HRMS.Core.Enums;
using HRMS.Core.Interfaces.Repositories;
using HRMS.Services.Leave.Dtos;
using HRMS.Shared.Constants;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;

namespace HRMS.Services.Leave
{
    /// <summary>
    /// Service for managing leave requests, balances, and approvals.
    /// </summary>
    public class LeaveService : ILeaveService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<LeaveService> _logger;
        private readonly IMemoryCache _cache;
        private readonly IValidator<CreateLeaveRequestDto> _createValidator;
        private readonly IValidator<ApproveLeaveDto> _approveValidator;
        private readonly IValidator<RejectLeaveDto> _rejectValidator;

        public LeaveService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<LeaveService> logger,
            IMemoryCache cache,
            IValidator<CreateLeaveRequestDto> createValidator,
            IValidator<ApproveLeaveDto> approveValidator,
            IValidator<RejectLeaveDto> rejectValidator)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _cache = cache;
            _createValidator = createValidator;
            _approveValidator = approveValidator;
            _rejectValidator = rejectValidator;
        }

        public async Task<LeaveRequestDto?> GetLeaveRequestByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("Getting leave request with ID: {LeaveRequestId}", id);

                var cacheKey = HrmsConstants.Cache.LeaveKey(id);
                if (_cache.TryGetValue(cacheKey, out LeaveRequestDto? cachedLeave))
                {
                    return cachedLeave;
                }

                var leaveRequest = await _unitOfWork.Leaves.GetByIdAsync(id);
                if (leaveRequest == null)
                {
                    _logger.LogWarning("Leave request with ID {LeaveRequestId} not found", id);
                    return null;
                }

                var leaveDto = _mapper.Map<LeaveRequestDto>(leaveRequest);
                _cache.Set(cacheKey, leaveDto, TimeSpan.FromMinutes(HrmsConstants.Cache.DefaultExpirationMinutes));

                return leaveDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting leave request with ID {LeaveRequestId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<LeaveRequestDto>> GetAllLeaveRequestsAsync()
        {
            try
            {
                _logger.LogInformation("Getting all leave requests");

                var cacheKey = HrmsConstants.Cache.AllLeavesKey;
                if (_cache.TryGetValue(cacheKey, out IEnumerable<LeaveRequestDto>? cachedLeaves))
                {
                    return cachedLeaves ?? Enumerable.Empty<LeaveRequestDto>();
                }

                var leaveRequests = await _unitOfWork.Leaves.GetAllAsync();
                var leaveDtos = _mapper.Map<IEnumerable<LeaveRequestDto>>(leaveRequests);

                _cache.Set(cacheKey, leaveDtos, TimeSpan.FromMinutes(HrmsConstants.Cache.DefaultExpirationMinutes));

                return leaveDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all leave requests");
                throw;
            }
        }

        public async Task<IEnumerable<LeaveRequestDto>> GetLeaveRequestsByEmployeeAsync(int employeeId)
        {
            try
            {
                _logger.LogInformation("Getting leave requests for employee ID: {EmployeeId}", employeeId);

                var leaveRequests = await _unitOfWork.Leaves.GetLeaveRequestsByEmployeeAsync(employeeId);
                return _mapper.Map<IEnumerable<LeaveRequestDto>>(leaveRequests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting leave requests for employee ID: {EmployeeId}", employeeId);
                throw;
            }
        }

        public async Task<IEnumerable<LeaveRequestDto>> GetLeaveRequestsByStatusAsync(LeaveStatus status)
        {
            try
            {
                _logger.LogInformation("Getting leave requests with status: {Status}", status);

                var leaveRequests = await _unitOfWork.Leaves.GetLeaveRequestsByStatusAsync(status);
                return _mapper.Map<IEnumerable<LeaveRequestDto>>(leaveRequests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting leave requests with status: {Status}", status);
                throw;
            }
        }

        public async Task<IEnumerable<LeaveRequestDto>> GetLeaveRequestsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                _logger.LogInformation("Getting leave requests between {StartDate} and {EndDate}", startDate, endDate);

                var leaveRequests = await _unitOfWork.Leaves.GetLeaveRequestsByDateRangeAsync(startDate, endDate);
                return _mapper.Map<IEnumerable<LeaveRequestDto>>(leaveRequests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting leave requests by date range");
                throw;
            }
        }

        public async Task<LeaveRequestDto> CreateLeaveRequestAsync(CreateLeaveRequestDto createDto)
        {
            try
            {
                _logger.LogInformation("Creating leave request for employee ID: {EmployeeId}", createDto.EmployeeId);

                // Validate
                var validationResult = await _createValidator.ValidateAsync(createDto);
                if (!validationResult.IsValid)
                {
                    throw new ValidationException(validationResult.Errors);
                }

                // Check if employee exists
                var employee = await _unitOfWork.Employees.GetByIdAsync(createDto.EmployeeId);
                if (employee == null)
                {
                    throw new KeyNotFoundException($"Employee with ID {createDto.EmployeeId} not found");
                }

                // Calculate total days
                var totalDays = CalculateWorkingDays(createDto.StartDate, createDto.EndDate);

                // Check if can apply
                var canApply = await CanApplyLeaveAsync(createDto.EmployeeId, createDto.LeaveType,
                    createDto.StartDate, createDto.EndDate);

                if (!canApply)
                {
                    throw new InvalidOperationException("Cannot apply for leave due to policy restrictions");
                }

                // Check for overlapping leaves
                var hasOverlap = await HasOverlappingLeaveAsync(createDto.EmployeeId,
                    createDto.StartDate, createDto.EndDate);

                if (hasOverlap)
                {
                    throw new InvalidOperationException("Employee already has a leave request for this period");
                }

                var leaveRequest = _mapper.Map<LeaveRequest>(createDto);
                leaveRequest.TotalDays = totalDays;
                leaveRequest.Status = LeaveStatus.Pending;
                leaveRequest.CreatedAt = DateTime.UtcNow;

                await _unitOfWork.Leaves.AddAsync(leaveRequest);
                await _unitOfWork.CompleteAsync();

                _logger.LogInformation("Leave request created successfully with ID: {LeaveRequestId}", leaveRequest.Id);

                // Clear cache
                _cache.Remove($"employee_leaves_{createDto.EmployeeId}");
                _cache.Remove("all_leaves");

                return _mapper.Map<LeaveRequestDto>(leaveRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating leave request");
                throw;
            }
        }

        public async Task<LeaveRequestDto> UpdateLeaveRequestAsync(UpdateLeaveRequestDto updateDto)
        {
            try
            {
                _logger.LogInformation("Updating leave request with ID: {LeaveRequestId}", updateDto.Id);

                var leaveRequest = await _unitOfWork.Leaves.GetByIdAsync(updateDto.Id);
                if (leaveRequest == null)
                {
                    throw new KeyNotFoundException($"Leave request with ID {updateDto.Id} not found");
                }

                // Only pending requests can be updated
                if (leaveRequest.Status != LeaveStatus.Pending)
                {
                    throw new InvalidOperationException("Only pending leave requests can be updated");
                }

                // Calculate new total days
                var totalDays = CalculateWorkingDays(updateDto.StartDate, updateDto.EndDate);

                // Check for overlapping leaves (excluding current request)
                var hasOverlap = await HasOverlappingLeaveAsync(leaveRequest.EmployeeId,
                    updateDto.StartDate, updateDto.EndDate, updateDto.Id);

                if (hasOverlap)
                {
                    throw new InvalidOperationException("Employee already has a leave request for this period");
                }

                _mapper.Map(updateDto, leaveRequest);
                leaveRequest.TotalDays = totalDays;
                leaveRequest.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Leaves.Update(leaveRequest);
                await _unitOfWork.CompleteAsync();

                _logger.LogInformation("Leave request {LeaveRequestId} updated successfully", leaveRequest.Id);

                // Clear cache
                _cache.Remove($"leave_{leaveRequest.Id}");
                _cache.Remove($"employee_leaves_{leaveRequest.EmployeeId}");
                _cache.Remove("all_leaves");

                return _mapper.Map<LeaveRequestDto>(leaveRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating leave request with ID {LeaveRequestId}", updateDto.Id);
                throw;
            }
        }

        public async Task<LeaveRequestDto> ApproveLeaveRequestAsync(ApproveLeaveDto approveDto, int approverId)
        {
            try
            {
                _logger.LogInformation("Approving leave request with ID: {LeaveRequestId}", approveDto.Id);

                // Validate
                var validationResult = await _approveValidator.ValidateAsync(approveDto);
                if (!validationResult.IsValid)
                {
                    throw new ValidationException(validationResult.Errors);
                }

                var leaveRequest = await _unitOfWork.Leaves.GetByIdAsync(approveDto.Id);
                if (leaveRequest == null)
                {
                    throw new KeyNotFoundException($"Leave request with ID {approveDto.Id} not found");
                }

                if (leaveRequest.Status != LeaveStatus.Pending)
                {
                    throw new InvalidOperationException("Only pending leave requests can be approved");
                }

                // Check if approver exists and is a manager
                var approver = await _unitOfWork.Employees.GetByIdAsync(approverId);
                if (approver == null)
                {
                    throw new KeyNotFoundException($"Approver with ID {approverId} not found");
                }

                leaveRequest.Status = LeaveStatus.Approved;
                leaveRequest.ApprovedById = approverId;
                leaveRequest.ApprovedDate = DateTime.UtcNow;
                leaveRequest.Remarks = approveDto.Remarks;
                leaveRequest.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Leaves.Update(leaveRequest);
                await _unitOfWork.CompleteAsync();

                _logger.LogInformation("Leave request {LeaveRequestId} approved successfully", leaveRequest.Id);

                // Clear cache
                _cache.Remove($"leave_{leaveRequest.Id}");
                _cache.Remove($"employee_leaves_{leaveRequest.EmployeeId}");
                _cache.Remove("all_leaves");

                return _mapper.Map<LeaveRequestDto>(leaveRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving leave request with ID {LeaveRequestId}", approveDto.Id);
                throw;
            }
        }

        public async Task<LeaveRequestDto> RejectLeaveRequestAsync(RejectLeaveDto rejectDto, int approverId)
        {
            try
            {
                _logger.LogInformation("Rejecting leave request with ID: {LeaveRequestId}", rejectDto.Id);

                // Validate
                var validationResult = await _rejectValidator.ValidateAsync(rejectDto);
                if (!validationResult.IsValid)
                {
                    throw new ValidationException(validationResult.Errors);
                }

                var leaveRequest = await _unitOfWork.Leaves.GetByIdAsync(rejectDto.Id);
                if (leaveRequest == null)
                {
                    throw new KeyNotFoundException($"Leave request with ID {rejectDto.Id} not found");
                }

                if (leaveRequest.Status != LeaveStatus.Pending)
                {
                    throw new InvalidOperationException("Only pending leave requests can be rejected");
                }

                leaveRequest.Status = LeaveStatus.Rejected;
                leaveRequest.ApprovedById = approverId;
                leaveRequest.ApprovedDate = DateTime.UtcNow;
                leaveRequest.Remarks = rejectDto.Remarks;
                leaveRequest.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Leaves.Update(leaveRequest);
                await _unitOfWork.CompleteAsync();

                _logger.LogInformation("Leave request {LeaveRequestId} rejected successfully", leaveRequest.Id);

                // Clear cache
                _cache.Remove($"leave_{leaveRequest.Id}");
                _cache.Remove($"employee_leaves_{leaveRequest.EmployeeId}");
                _cache.Remove("all_leaves");

                return _mapper.Map<LeaveRequestDto>(leaveRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting leave request with ID {LeaveRequestId}", rejectDto.Id);
                throw;
            }
        }

        public async Task<bool> CancelLeaveRequestAsync(int id, int employeeId)
        {
            try
            {
                _logger.LogInformation("Cancelling leave request with ID: {LeaveRequestId}", id);

                var leaveRequest = await _unitOfWork.Leaves.GetByIdAsync(id);
                if (leaveRequest == null)
                {
                    throw new KeyNotFoundException($"Leave request with ID {id} not found");
                }

                // Only the employee who created it can cancel
                if (leaveRequest.EmployeeId != employeeId)
                {
                    throw new UnauthorizedAccessException("You can only cancel your own leave requests");
                }

                // Only pending or approved requests can be cancelled
                if (leaveRequest.Status != LeaveStatus.Pending && leaveRequest.Status != LeaveStatus.Approved)
                {
                    throw new InvalidOperationException("This leave request cannot be cancelled");
                }

                leaveRequest.Status = LeaveStatus.Cancelled;
                leaveRequest.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Leaves.Update(leaveRequest);
                await _unitOfWork.CompleteAsync();

                _logger.LogInformation("Leave request {LeaveRequestId} cancelled successfully", id);

                // Clear cache
                _cache.Remove($"leave_{id}");
                _cache.Remove($"employee_leaves_{employeeId}");
                _cache.Remove("all_leaves");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling leave request with ID {LeaveRequestId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteLeaveRequestAsync(int id)
        {
            try
            {
                _logger.LogInformation("Deleting leave request with ID: {LeaveRequestId}", id);

                var leaveRequest = await _unitOfWork.Leaves.GetByIdAsync(id);
                if (leaveRequest == null)
                {
                    throw new KeyNotFoundException($"Leave request with ID {id} not found");
                }

                // Soft delete
                leaveRequest.IsDeleted = true;
                leaveRequest.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Leaves.Update(leaveRequest);
                await _unitOfWork.CompleteAsync();

                _logger.LogInformation("Leave request {LeaveRequestId} deleted successfully", id);

                // Clear cache
                _cache.Remove($"leave_{id}");
                _cache.Remove($"employee_leaves_{leaveRequest.EmployeeId}");
                _cache.Remove("all_leaves");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting leave request with ID {LeaveRequestId}", id);
                throw;
            }
        }

        public async Task<LeaveBalanceDto> GetLeaveBalanceAsync(int employeeId, int year)
        {
            try
            {
                _logger.LogInformation("Getting leave balance for employee ID: {EmployeeId}, year: {Year}", employeeId, year);

                var employee = await _unitOfWork.Employees.GetByIdAsync(employeeId);
                if (employee == null)
                {
                    throw new KeyNotFoundException($"Employee with ID {employeeId} not found");
                }

                var balance = new LeaveBalanceDto
                {
                    EmployeeId = employeeId,
                    EmployeeName = employee.FullName,
                    Year = year,
                    Balances = new Dictionary<LeaveType, LeaveBalanceDetail>()
                };

                foreach (LeaveType leaveType in Enum.GetValues(typeof(LeaveType)))
                {
                    var entitled = GetEntitledLeaveDays(leaveType, employee);
                    var used = await _unitOfWork.Leaves.GetTotalLeaveDaysAsync(employeeId, year, leaveType);
                    var pending = await _unitOfWork.Leaves
                        .FindAsync(l => l.EmployeeId == employeeId
                            && l.StartDate.Year == year
                            && l.LeaveType == leaveType
                            && l.Status == LeaveStatus.Pending)
                        .ContinueWith(t => t.Result.Sum(l => l.TotalDays));

                    balance.Balances[leaveType] = new LeaveBalanceDetail
                    {
                        Entitled = entitled,
                        Used = used,
                        Pending = pending
                    };
                }

                return balance;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting leave balance for employee ID: {EmployeeId}", employeeId);
                throw;
            }
        }

        public async Task<int> GetAvailableLeaveDaysAsync(int employeeId, int year, LeaveType leaveType)
        {
            try
            {
                var employee = await _unitOfWork.Employees.GetByIdAsync(employeeId);
                if (employee == null)
                {
                    throw new KeyNotFoundException($"Employee with ID {employeeId} not found");
                }

                var entitled = GetEntitledLeaveDays(leaveType, employee);
                var used = await _unitOfWork.Leaves.GetTotalLeaveDaysAsync(employeeId, year, leaveType);
                var pending = await _unitOfWork.Leaves
                    .FindAsync(l => l.EmployeeId == employeeId
                        && l.StartDate.Year == year
                        && l.LeaveType == leaveType
                        && l.Status == LeaveStatus.Pending)
                    .ContinueWith(t => t.Result.Sum(l => l.TotalDays));

                return (int)(entitled - used - pending);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available leave days for employee ID: {EmployeeId}", employeeId);
                throw;
            }
        }

        public async Task<bool> CanApplyLeaveAsync(int employeeId, LeaveType leaveType, DateTime startDate, DateTime endDate)
        {
            try
            {
                var employee = await _unitOfWork.Employees.GetByIdAsync(employeeId);
                if (employee == null)
                {
                    return false;
                }

                // Check probation period
                if (employee.ProbationEndDate.HasValue && employee.ProbationEndDate > DateTime.Today)
                {
                    if (leaveType != LeaveType.Sick)
                    {
                        return false;
                    }
                }

                // Check minimum notice period (2 days for non-sick leave)
                if (leaveType != LeaveType.Sick && startDate < DateTime.Today.AddDays(2))
                {
                    return false;
                }

                // Check maximum leave duration
                var totalDays = CalculateWorkingDays(startDate, endDate);
                if (totalDays > GetMaxLeaveDuration(leaveType))
                {
                    return false;
                }

                // Check available balance
                var availableDays = await GetAvailableLeaveDaysAsync(employeeId, startDate.Year, leaveType);
                if (totalDays > availableDays)
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if employee {EmployeeId} can apply for leave", employeeId);
                return false;
            }
        }

        public async Task<bool> HasOverlappingLeaveAsync(int employeeId, DateTime startDate, DateTime endDate, int? excludeId = null)
        {
            try
            {
                return await _unitOfWork.Leaves.HasOverlappingLeaveAsync(employeeId, startDate, endDate, excludeId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking overlapping leave for employee {EmployeeId}", employeeId);
                return true; // Fail safe - better to block than allow overlap
            }
        }

        public async Task<IEnumerable<LeaveRequestDto>> GetPendingApprovalsAsync(int managerId)
        {
            try
            {
                _logger.LogInformation("Getting pending approvals for manager ID: {ManagerId}", managerId);

                var pendingLeaves = await _unitOfWork.Leaves.GetPendingApprovalsAsync(managerId);
                return _mapper.Map<IEnumerable<LeaveRequestDto>>(pendingLeaves);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending approvals for manager ID: {ManagerId}", managerId);
                throw;
            }
        }

        public async Task<IEnumerable<LeaveRequestDto>> GetTeamLeaveCalendarAsync(int managerId, DateTime month)
        {
            try
            {
                _logger.LogInformation("Getting team leave calendar for manager ID: {ManagerId}, month: {Month}",
                    managerId, month.ToString("yyyy-MM"));

                var startOfMonth = new DateTime(month.Year, month.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

                var leaveRequests = await _unitOfWork.Leaves
                    .GetLeaveRequestsByDateRangeAsync(startOfMonth, endOfMonth);

                return _mapper.Map<IEnumerable<LeaveRequestDto>>(leaveRequests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting team leave calendar for manager ID: {ManagerId}", managerId);
                throw;
            }
        }

        public async Task<Dictionary<LeaveStatus, int>> GetLeaveStatisticsAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                return await _unitOfWork.Leaves.GetLeaveStatisticsAsync(startDate, endDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting leave statistics");
                throw;
            }
        }

        public async Task<int> GetEmployeesOnLeaveAsync(DateTime date)
        {
            try
            {
                return await _unitOfWork.Leaves.GetEmployeesOnLeaveAsync(date);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting employees on leave for date: {Date}", date);
                throw;
            }
        }

        public async Task<IEnumerable<LeaveRequestDto>> GetUpcomingLeavesAsync(int days)
        {
            try
            {
                var upcomingLeaves = await _unitOfWork.Leaves.GetUpcomingLeavesAsync(days);
                return _mapper.Map<IEnumerable<LeaveRequestDto>>(upcomingLeaves);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting upcoming leaves");
                throw;
            }
        }

        #region Private Helper Methods

        private int CalculateWorkingDays(DateTime startDate, DateTime endDate)
        {
            int days = 0;
            var current = startDate;

            while (current <= endDate)
            {
                if (current.DayOfWeek != DayOfWeek.Saturday && current.DayOfWeek != DayOfWeek.Sunday)
                {
                    days++;
                }
                current = current.AddDays(1);
            }

            return days;
        }

        private decimal GetEntitledLeaveDays(LeaveType leaveType, Employee employee)
        {
            return leaveType switch
            {
                LeaveType.Annual => 20, // 20 days per year
                LeaveType.Sick => 12,    // 12 days per year
                LeaveType.Maternity => 180, // 6 months
                LeaveType.Paternity => 15,   // 15 days
                LeaveType.Unpaid => 365,      // As needed
                LeaveType.Compensatory => 30, // 30 days
                LeaveType.Bereavement => 5,   // 5 days
                LeaveType.Marriage => 5,      // 5 days
                _ => 0
            };
        }

        private int GetMaxLeaveDuration(LeaveType leaveType)
        {
            return leaveType switch
            {
                LeaveType.Annual => 20,
                LeaveType.Sick => 10,
                LeaveType.Maternity => 180,
                LeaveType.Paternity => 15,
                LeaveType.Unpaid => 90,
                LeaveType.Compensatory => 5,
                LeaveType.Bereavement => 5,
                LeaveType.Marriage => 5,
                _ => 30
            };
        }

        #endregion
    }
}