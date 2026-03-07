using HRMS.Infrastructure.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HRMS.UnitTests.Infrastructure.Services
{
    /// <summary>
    /// Unit tests for <see cref="DistributedCacheService"/>.
    /// An in-memory <see cref="IDistributedCache"/> implementation is used so these tests
    /// are fast and require no external infrastructure.
    /// </summary>
    public class DistributedCacheServiceTests
    {
        private readonly DistributedCacheService _sut;

        public DistributedCacheServiceTests()
        {
            // Use the built-in in-process IDistributedCache (no Redis required).
            IDistributedCache inner = new MemoryDistributedCache(
                Options.Create(new MemoryDistributedCacheOptions()));

            _sut = new DistributedCacheService(
                inner,
                NullLogger<DistributedCacheService>.Instance);
        }

        // ──────────────────────────── SetAsync / GetAsync ───────────────────────

        [Fact]
        public async Task SetAsync_ThenGetAsync_ReturnsStoredValue()
        {
            // Arrange
            var payload = new CachePayload { Name = "Alice", Value = 42 };

            // Act
            await _sut.SetAsync("key1", payload);
            var result = await _sut.GetAsync<CachePayload>("key1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Alice", result.Name);
            Assert.Equal(42, result.Value);
        }

        [Fact]
        public async Task GetAsync_WhenKeyDoesNotExist_ReturnsNull()
        {
            var result = await _sut.GetAsync<CachePayload>("missing_key");
            Assert.Null(result);
        }

        [Fact]
        public async Task SetAsync_WithAbsoluteExpiration_ExpiresAfterTimeout()
        {
            // Arrange
            var payload = new CachePayload { Name = "Bob", Value = 1 };

            // Act – set with 100 ms expiry
            await _sut.SetAsync("expiring_key", payload, absoluteExpiration: TimeSpan.FromMilliseconds(100));

            // Assert – still available immediately
            var before = await _sut.GetAsync<CachePayload>("expiring_key");
            Assert.NotNull(before);

            // Wait for expiry then assert it is gone
            await Task.Delay(200);
            var after = await _sut.GetAsync<CachePayload>("expiring_key");
            Assert.Null(after);
        }

        // ──────────────────────────── RemoveAsync ────────────────────────────────

        [Fact]
        public async Task RemoveAsync_DeletesExistingEntry()
        {
            // Arrange
            var payload = new CachePayload { Name = "Carol", Value = 99 };
            await _sut.SetAsync("remove_key", payload);

            // Act
            await _sut.RemoveAsync("remove_key");

            // Assert
            var result = await _sut.GetAsync<CachePayload>("remove_key");
            Assert.Null(result);
        }

        [Fact]
        public async Task RemoveAsync_OnNonExistentKey_DoesNotThrow()
        {
            // Should not throw even if the key was never set.
            var exception = await Record.ExceptionAsync(() => _sut.RemoveAsync("ghost_key"));
            Assert.Null(exception);
        }

        // ──────────────────────────── ExistsAsync ────────────────────────────────

        [Fact]
        public async Task ExistsAsync_WhenKeyExists_ReturnsTrue()
        {
            await _sut.SetAsync("exists_key", new CachePayload { Name = "Dave" });
            Assert.True(await _sut.ExistsAsync("exists_key"));
        }

        [Fact]
        public async Task ExistsAsync_WhenKeyDoesNotExist_ReturnsFalse()
        {
            Assert.False(await _sut.ExistsAsync("no_such_key"));
        }

        // ──────────────────────────── Overwrite ──────────────────────────────────

        [Fact]
        public async Task SetAsync_OverwritesExistingEntry()
        {
            // Arrange
            await _sut.SetAsync("overwrite_key", new CachePayload { Name = "V1", Value = 1 });

            // Act
            await _sut.SetAsync("overwrite_key", new CachePayload { Name = "V2", Value = 2 });

            // Assert
            var result = await _sut.GetAsync<CachePayload>("overwrite_key");
            Assert.NotNull(result);
            Assert.Equal("V2", result.Name);
            Assert.Equal(2, result.Value);
        }

        // ──────────────────────────── Helper type ────────────────────────────────

        private sealed class CachePayload
        {
            public string? Name { get; init; }
            public int Value { get; init; }
        }
    }
}
