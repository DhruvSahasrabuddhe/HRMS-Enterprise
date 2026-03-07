using HRMS.Core.Interfaces.Services;
using HRMS.Shared.Constants;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HRMS.Infrastructure.Services
{
    /// <summary>
    /// <see cref="IDistributedCacheService"/> implementation backed by
    /// <see cref="IDistributedCache"/> (Redis, SQL Server, or in-process fallback).
    /// Serialization uses <see cref="System.Text.Json"/>.
    /// </summary>
    public class DistributedCacheService : IDistributedCacheService
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<DistributedCacheService> _logger;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public DistributedCacheService(
            IDistributedCache cache,
            ILogger<DistributedCacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
            where T : class
        {
            try
            {
                var bytes = await _cache.GetAsync(key, cancellationToken);
                if (bytes is null || bytes.Length == 0)
                    return null;

                return JsonSerializer.Deserialize<T>(bytes, _jsonOptions);
            }
            catch (Exception ex)
            {
                // Cache read failures must never bubble up and break the application.
                _logger.LogWarning(ex, "Distributed cache GET failed for key '{CacheKey}'", key);
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task SetAsync<T>(
            string key,
            T value,
            TimeSpan? absoluteExpiration = null,
            TimeSpan? slidingExpiration = null,
            CancellationToken cancellationToken = default)
            where T : class
        {
            try
            {
                var options = new DistributedCacheEntryOptions();

                if (absoluteExpiration.HasValue)
                    options.AbsoluteExpirationRelativeToNow = absoluteExpiration;
                else
                    options.AbsoluteExpirationRelativeToNow =
                        TimeSpan.FromMinutes(HrmsConstants.Cache.DefaultExpirationMinutes);

                if (slidingExpiration.HasValue)
                    options.SlidingExpiration = slidingExpiration;

                var bytes = JsonSerializer.SerializeToUtf8Bytes(value, _jsonOptions);
                await _cache.SetAsync(key, bytes, options, cancellationToken);
            }
            catch (Exception ex)
            {
                // Cache write failures must never bubble up and break the application.
                _logger.LogWarning(ex, "Distributed cache SET failed for key '{CacheKey}'", key);
            }
        }

        /// <inheritdoc/>
        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                await _cache.RemoveAsync(key, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Distributed cache REMOVE failed for key '{CacheKey}'", key);
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                var bytes = await _cache.GetAsync(key, cancellationToken);
                return bytes is not null && bytes.Length > 0;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Distributed cache EXISTS failed for key '{CacheKey}'", key);
                return false;
            }
        }
    }
}
