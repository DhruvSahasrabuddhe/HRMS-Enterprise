namespace HRMS.Core.Interfaces.Services
{
    /// <summary>
    /// Abstraction over a distributed (or in-process) cache that supports typed get/set/remove
    /// operations with optional absolute and sliding expiration.
    /// </summary>
    public interface IDistributedCacheService
    {
        /// <summary>
        /// Attempts to retrieve a cached value by <paramref name="key"/>.
        /// Returns <c>null</c> when the key is not found or has expired.
        /// </summary>
        /// <typeparam name="T">Type of the cached value.</typeparam>
        /// <param name="key">Cache key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// Stores <paramref name="value"/> in the cache under <paramref name="key"/>.
        /// </summary>
        /// <typeparam name="T">Type of the value to cache.</typeparam>
        /// <param name="key">Cache key.</param>
        /// <param name="value">Value to cache.</param>
        /// <param name="absoluteExpiration">
        /// Optional absolute expiration.  When <c>null</c> a sensible default is applied.
        /// </param>
        /// <param name="slidingExpiration">
        /// Optional sliding expiration.  When provided, the entry is evicted if it has not been
        /// accessed within this window, even if <paramref name="absoluteExpiration"/> has not elapsed.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task SetAsync<T>(
            string key,
            T value,
            TimeSpan? absoluteExpiration = null,
            TimeSpan? slidingExpiration = null,
            CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// Removes the entry associated with <paramref name="key"/> from the cache.
        /// </summary>
        /// <param name="key">Cache key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task RemoveAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns <c>true</c> when a non-expired entry exists for <paramref name="key"/>.
        /// </summary>
        /// <param name="key">Cache key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
    }
}
