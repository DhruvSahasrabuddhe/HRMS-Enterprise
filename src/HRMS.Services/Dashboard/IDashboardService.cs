using HRMS.Services.Dashboard.Dtos;

namespace HRMS.Services.Dashboard
{
    /// <summary>
    /// Service interface for managing dashboard data and statistics.
    /// </summary>
    public interface IDashboardService
    {
        /// <summary>
        /// Retrieves comprehensive dashboard data including employee counts, leave statistics, and recent activities.
        /// </summary>
        /// <returns>The dashboard data transfer object containing all dashboard information.</returns>
        Task<DashboardDto> GetDashboardDataAsync();

        /// <summary>
        /// Retrieves statistical data for the dashboard including employee counts by department.
        /// </summary>
        /// <returns>An object containing various statistics.</returns>
        Task<object> GetStatisticsAsync();

        /// <summary>
        /// Retrieves employee trend data for chart visualization over the last 6 months.
        /// </summary>
        /// <returns>Chart data containing labels and dataset values.</returns>
        Task<ChartDataDto> GetEmployeeChartDataAsync();

        /// <summary>
        /// Retrieves recent activities such as new hires and leave requests.
        /// </summary>
        /// <returns>A list of recent activities with details and timestamps.</returns>
        Task<List<RecentActivityDto>> GetRecentActivitiesAsync();
    }
}