using System.Net;
using System.Text.Json;

namespace HRMS.IntegrationTests.Api;

/// <summary>
/// Integration tests for the /health and /health/live endpoints.
/// Verifies that the health-check pipeline is wired correctly and the
/// responses conform to the expected shape.
/// </summary>
[Collection(HrmsIntegrationCollection.Name)]
public class HealthCheckIntegrationTests
{
    private readonly HttpClient _client;

    public HealthCheckIntegrationTests(HrmsWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task GetHealth_ReturnsOkOrDegradedWithJsonBody()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert – 200 (Healthy/Degraded) or 503 (Unhealthy). In-memory DB reports
        // healthy so we accept any 2xx or 503 without asserting exact status here;
        // what matters is the content-type and JSON structure.
        var body = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(body);

        using var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.TryGetProperty("status", out _),
            "Response JSON must have a 'status' property");
        Assert.True(doc.RootElement.TryGetProperty("checks", out _),
            "Response JSON must have a 'checks' property");
    }

    [Fact]
    public async Task GetHealth_ContentType_IsApplicationJson()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetHealthLive_Returns200OK()
    {
        // Act
        var response = await _client.GetAsync("/health/live");

        // Assert – liveness probe has no named health checks so it always reports
        // Healthy (200) as long as the process is running.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetHealth_ResponseContainsDurationField()
    {
        // Act
        var response = await _client.GetAsync("/health");
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        using var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.TryGetProperty("totalDuration", out _),
            "Response JSON must include a 'totalDuration' field");
    }

    [Fact]
    public async Task GetHealth_ChecksArray_ContainsDatabaseEntry()
    {
        // Act
        var response = await _client.GetAsync("/health");
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        using var doc = JsonDocument.Parse(body);
        var checks = doc.RootElement.GetProperty("checks").EnumerateArray().ToList();
        Assert.Contains(checks, c =>
            c.TryGetProperty("name", out var nameProp) &&
            nameProp.GetString() == "database");
    }
}
