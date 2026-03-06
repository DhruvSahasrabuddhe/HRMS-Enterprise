using AutoMapper;
using HRMS.Core.Enums;
using HRMS.Core.Interfaces.Repositories;
using HRMS.Core.Interfaces.Services;
using HRMS.Services.Dashboard.Dtos;
using HRMS.Shared.Constants;
using Microsoft.Extensions.Logging;

namespace HRMS.Services.Dashboard
{
    /// <summary>
    /// Service for managing dashboard data and statistics.
    /// </summary>
    public class DashboardService : IDashboardService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<DashboardService> _logger;
        private readonly IDateTimeProvider _dateTimeProvider;

        public DashboardService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<DashboardService> logger,
            IDateTimeProvider dateTimeProvider)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<DashboardDto> GetDashboardDataAsync()
        {
            try
            {
                _logger.LogInformation("Loading dashboard data");

                var totalEmployees = await _unitOfWork.Employees.CountAsync(e => !e.IsDeleted);
                var activeEmployees = await _unitOfWork.Employees.CountAsync(e => e.Status == EmployeeStatus.Active);
                var totalDepartments = await _unitOfWork.Departments.CountAsync(d => !d.IsDeleted);
                var employeesOnLeave = await _unitOfWork.Leaves.GetEmployeesOnLeaveAsync(_dateTimeProvider.Today);
                var pendingLeaves = await _unitOfWork.Leaves.CountAsync(l => l.Status == LeaveStatus.Pending);

                var startOfMonth = new DateTime(_dateTimeProvider.Today.Year, _dateTimeProvider.Today.Month, 1);
                var newHires = await _unitOfWork.Employees.CountAsync(e => e.HireDate >= startOfMonth);

                return new DashboardDto
                {
                    TotalEmployees = totalEmployees,
                    ActiveEmployees = activeEmployees,
                    TotalDepartments = totalDepartments,
                    EmployeesOnLeave = employeesOnLeave,
                    PendingLeaveRequests = pendingLeaves,
                    NewHiresThisMonth = newHires,
                    RecentActivities = await GetRecentActivitiesAsync(),
                    DepartmentStats = await GetDepartmentStatsAsync(),
                    LeaveStats = await GetLeaveStatsAsync()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard data");
                throw;
            }
        }

        public async Task<object> GetStatisticsAsync()
        {
            var employeeCountByDepartment = await _unitOfWork.Employees
                .FindAsync(e => !e.IsDeleted)
                .ContinueWith(t => t.Result
                    .GroupBy(e => e.Department?.Name ?? "Unassigned")
                    .Select(g => new { Department = g.Key, Count = g.Count() }));

            return new
            {
                employeeCountByDepartment,
                totalEmployees = await _unitOfWork.Employees.CountAsync(e => !e.IsDeleted),
                employeesOnLeave = await _unitOfWork.Leaves.GetEmployeesOnLeaveAsync(_dateTimeProvider.Today)
            };
        }

        public async Task<ChartDataDto> GetEmployeeChartDataAsync()
        {
            var last6Months = Enumerable.Range(0, HrmsConstants.Dashboard.ChartMonthsToShow)
                .Select(i => _dateTimeProvider.Today.AddMonths(-i))
                .OrderBy(d => d)
                .ToList();

            var labels = last6Months.Select(d => d.ToString("MMM yyyy")).ToArray();
            var values = new List<decimal>();

            foreach (var month in last6Months)
            {
                var startOfMonth = new DateTime(month.Year, month.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

                var count = await _unitOfWork.Employees
                    .CountAsync(e => e.HireDate <= endOfMonth && !e.IsDeleted);
                values.Add(count);
            }

            return new ChartDataDto
            {
                Labels = labels,
                Values = values.ToArray(),
                Colors = new[] { HrmsConstants.UI.PrimaryChartColor }
            };
        }

        public async Task<List<RecentActivityDto>> GetRecentActivitiesAsync()
        {
            var activities = new List<RecentActivityDto>();

            // Get recent new hires
            var recentHires = await _unitOfWork.Employees
                .FindAsync(e => e.HireDate >= _dateTimeProvider.Today.AddDays(-HrmsConstants.Dashboard.RecentActivityDays));

            foreach (var hire in recentHires.Take(HrmsConstants.Dashboard.RecentActivityHiresLimit))
            {
                activities.Add(new RecentActivityDto
                {
                    ActivityType = HrmsConstants.ActivityTypes.NewHire,
                    Description = $"{hire.FullName} joined as {hire.JobTitle}",
                    Timestamp = hire.HireDate,
                    Icon = "user-plus",
                    Color = "success"
                });
            }

            // Get recent leave requests
            var recentLeaves = await _unitOfWork.Leaves
                .FindAsync(l => l.CreatedAt >= _dateTimeProvider.Today.AddDays(-HrmsConstants.Dashboard.UpcomingLeavesDays));

            foreach (var leave in recentLeaves.Take(HrmsConstants.Dashboard.RecentActivityLeavesLimit))
            {
                activities.Add(new RecentActivityDto
                {
                    ActivityType = HrmsConstants.ActivityTypes.LeaveRequest,
                    Description = $"{leave.Employee?.FullName} requested {leave.LeaveType} leave",
                    Timestamp = leave.CreatedAt,
                    Icon = "calendar-alt",
                    Color = "info"
                });
            }

            return activities.OrderByDescending(a => a.Timestamp).ToList();
        }

        private async Task<List<DepartmentStatsDto>> GetDepartmentStatsAsync()
        {
            var departments = await _unitOfWork.Departments
                .GetDepartmentsWithEmployeeCountAsync();

            return departments.Select(d => new DepartmentStatsDto
            {
                DepartmentName = d.Name,
                EmployeeCount = d.Employees?.Count ?? 0,
                BudgetUtilization = d.Budget.HasValue && d.Budget > 0
                    ? ((d.Employees?.Sum(e => e.Salary) ?? 0) / d.Budget.Value) * 100
                    : 0
            }).ToList();
        }

        private async Task<List<LeaveStatsDto>> GetLeaveStatsAsync()
        {
            var startOfMonth = new DateTime(_dateTimeProvider.Today.Year, _dateTimeProvider.Today.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            var leaveRequests = await _unitOfWork.Leaves
                .FindAsync(l => l.StartDate >= startOfMonth && l.EndDate <= endOfMonth);

            return leaveRequests
                .GroupBy(l => l.LeaveType)
                .Select(g => new LeaveStatsDto
                {
                    LeaveType = g.Key,
                    RequestCount = g.Count(),
                    TotalDays = g.Sum(l => l.TotalDays)
                })
                .ToList();
        }
    }
}