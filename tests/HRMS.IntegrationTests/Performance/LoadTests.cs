using System.Diagnostics;
using System.Net;

namespace HRMS.IntegrationTests.Performance;

/// <summary>
/// Load and performance tests that exercise the application under concurrent
/// requests and verify that response times remain within acceptable thresholds.
///
/// These tests use the <see cref="HrmsWebApplicationFactory"/> in-process test
/// server so no external infrastructure is required.
/// </summary>
[Collection(HrmsIntegrationCollection.Name)]
public class LoadTests
{
    private readonly HrmsWebApplicationFactory _factory;

    // Acceptable upper-bound response times for each category of endpoint
    private const int HealthEndpointMaxMs = 2_000;
    private const int ConcurrentRequests = 20;

    public LoadTests(HrmsWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ── Health endpoint response time ─────────────────────────────────────────────

    [Fact]
    public async Task GetHealthLive_SingleRequest_RespondsWithinThreshold()
    {
        // Arrange
        var client = _factory.CreateClient();
        var sw = Stopwatch.StartNew();

        // Act
        var response = await client.GetAsync("/health/live");
        sw.Stop();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(sw.ElapsedMilliseconds < HealthEndpointMaxMs,
            $"Response time {sw.ElapsedMilliseconds} ms exceeded threshold {HealthEndpointMaxMs} ms");
    }

    [Fact]
    public async Task GetHealth_SingleRequest_RespondsWithinThreshold()
    {
        // Arrange
        var client = _factory.CreateClient();
        var sw = Stopwatch.StartNew();

        // Act
        await client.GetAsync("/health");
        sw.Stop();

        // Assert
        Assert.True(sw.ElapsedMilliseconds < HealthEndpointMaxMs,
            $"Response time {sw.ElapsedMilliseconds} ms exceeded threshold {HealthEndpointMaxMs} ms");
    }

    // ── Concurrent health-check requests ─────────────────────────────────────────

    [Fact]
    public async Task GetHealthLive_ConcurrentRequests_AllSucceed()
    {
        // Arrange
        var tasks = Enumerable.Range(0, ConcurrentRequests)
            .Select(_ =>
            {
                // Each task creates its own HttpClient to avoid shared state
                var client = _factory.CreateClient();
                return client.GetAsync("/health/live");
            })
            .ToList();

        // Act
        var responses = await Task.WhenAll(tasks);

        // Assert – every response must be successful
        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
    }

    [Fact]
    public async Task GetHealthLive_ConcurrentRequests_AverageResponseTimeWithinThreshold()
    {
        // Arrange
        var sw = Stopwatch.StartNew();
        var tasks = Enumerable.Range(0, ConcurrentRequests)
            .Select(_ =>
            {
                var client = _factory.CreateClient();
                return client.GetAsync("/health/live");
            })
            .ToList();

        // Act
        await Task.WhenAll(tasks);
        sw.Stop();

        // Assert – average time per request
        var avgMs = sw.ElapsedMilliseconds / (double)ConcurrentRequests;
        Assert.True(avgMs < HealthEndpointMaxMs,
            $"Average response time {avgMs:F1} ms exceeded threshold {HealthEndpointMaxMs} ms");
    }

    // ── Sequential throughput ────────────────────────────────────────────────────

    [Fact]
    public async Task GetHealthLive_RepeatedSequentialRequests_CompleteWithinTotalBudget()
    {
        // Arrange
        const int requestCount = 10;
        const int totalBudgetMs = requestCount * HealthEndpointMaxMs;
        var client = _factory.CreateClient();
        var sw = Stopwatch.StartNew();

        // Act
        for (int i = 0; i < requestCount; i++)
        {
            var response = await client.GetAsync("/health/live");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        sw.Stop();

        // Assert
        Assert.True(sw.ElapsedMilliseconds < totalBudgetMs,
            $"Total elapsed {sw.ElapsedMilliseconds} ms exceeded budget {totalBudgetMs} ms");
    }

    // ── Concurrent mixed endpoints ────────────────────────────────────────────────

    [Fact]
    public async Task MixedEndpoints_ConcurrentRequests_AllReturnExpectedStatusCodes()
    {
        // Arrange – mix of public (health) and protected (Employee) endpoints
        var endpoints = new[]
        {
            ("/health/live", HttpStatusCode.OK),
            ("/health", HttpStatusCode.OK),
            ("/Employee", HttpStatusCode.Redirect),         // auth redirect
            ("/Department", HttpStatusCode.Redirect),       // auth redirect
        };

        var clientOptions = new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        };

        var tasks = endpoints
            .SelectMany(ep => Enumerable.Range(0, 5).Select(_ =>
            {
                var client = _factory.CreateClient(clientOptions);
                return (ep.Item2, client.GetAsync(ep.Item1));
            }))
            .ToList();

        // Act
        var results = await Task.WhenAll(tasks.Select(async t =>
        {
            var response = await t.Item2;
            return (Expected: t.Item1, Actual: response.StatusCode);
        }));

        // Assert
        Assert.All(results, r => Assert.Equal(r.Expected, r.Actual));
    }
}
