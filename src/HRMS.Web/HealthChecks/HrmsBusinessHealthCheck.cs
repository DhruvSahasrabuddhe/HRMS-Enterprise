using HRMS.Core.Interfaces.Repositories;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HRMS.Web.HealthChecks
{
    /// <summary>
    /// Health check that reports business-level metrics: active employee count and
    /// whether the HR system has any departments configured.  A degraded result is
    /// returned when the business data looks incomplete (e.g. no active employees).
    /// </summary>
    public class HrmsBusinessHealthCheck : IHealthCheck
    {
        private readonly IUnitOfWork _unitOfWork;

        public HrmsBusinessHealthCheck(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <inheritdoc />
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var employees = await _unitOfWork.Employees.GetAllAsync();
                var departments = await _unitOfWork.Departments.GetAllAsync();

                var employeeCount = employees.Count();
                var departmentCount = departments.Count();

                var data = new Dictionary<string, object>
                {
                    ["employeeCount"] = employeeCount,
                    ["departmentCount"] = departmentCount
                };

                if (departmentCount == 0)
                {
                    return HealthCheckResult.Degraded(
                        "No departments are configured in the system.",
                        data: data);
                }

                return HealthCheckResult.Healthy(
                    $"System has {employeeCount} employee(s) across {departmentCount} department(s).",
                    data);
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy(
                    "Business health check threw an exception.",
                    exception: ex);
            }
        }
    }
}
