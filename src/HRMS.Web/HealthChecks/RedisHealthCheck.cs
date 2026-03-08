using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace HRMS.Web.HealthChecks
{
    /// <summary>
    /// Health check that verifies connectivity to the Redis distributed cache.
    /// When no Redis connection string is configured the check is skipped and
    /// reports healthy (the application falls back to the in-process cache).
    /// Uses a shared <see cref="IConnectionMultiplexer"/> registered in DI to
    /// avoid the overhead of opening a new connection on every health check.
    /// </summary>
    public class RedisHealthCheck : IHealthCheck
    {
        private readonly IConnectionMultiplexer? _redis;

        public RedisHealthCheck(IConnectionMultiplexer? redis = null)
        {
            _redis = redis;
        }

        /// <inheritdoc />
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            if (_redis is null)
            {
                return HealthCheckResult.Healthy(
                    "Redis is not configured – using in-process distributed cache.");
            }

            try
            {
                var db = _redis.GetDatabase();
                var pingResult = await db.PingAsync();

                return HealthCheckResult.Healthy(
                    $"Redis connection is healthy. Round-trip: {pingResult.TotalMilliseconds:F1} ms",
                    data: new Dictionary<string, object>
                    {
                        ["roundTripMs"] = pingResult.TotalMilliseconds
                    });
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy(
                    "Redis health check failed.",
                    exception: ex);
            }
        }
    }
}
