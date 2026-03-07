using HRMS.Infrastructure.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HRMS.Web.HealthChecks
{
    /// <summary>
    /// Health check that verifies the application can open a connection to the
    /// SQL Server database.  Uses EF Core's underlying connection so the check
    /// exercises the same connection-string and retry policy used by the application.
    /// </summary>
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly ApplicationDbContext _dbContext;

        public DatabaseHealthCheck(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <inheritdoc />
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);

                return canConnect
                    ? HealthCheckResult.Healthy("Database connection is healthy.")
                    : HealthCheckResult.Unhealthy("Unable to connect to the database.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy(
                    "Database health check threw an exception.",
                    exception: ex);
            }
        }
    }
}
