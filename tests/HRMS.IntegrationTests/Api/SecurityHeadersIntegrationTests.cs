using System.Net;

namespace HRMS.IntegrationTests.Api;

/// <summary>
/// Integration tests that verify every HTTP response carries the mandatory
/// security headers configured in <see cref="HRMS.Web.Middleware.SecurityHeadersMiddleware"/>.
/// </summary>
[Collection(HrmsIntegrationCollection.Name)]
public class SecurityHeadersIntegrationTests
{
    private readonly HttpClient _client;

    public SecurityHeadersIntegrationTests(HrmsWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    // ── Helper ────────────────────────────────────────────────────────────────────

    private async Task<HttpResponseMessage> GetResponseAsync(string path = "/health/live")
        => await _client.GetAsync(path);

    // ── X-Frame-Options ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Response_ContainsXFrameOptions_DenyHeader()
    {
        // Act
        var response = await GetResponseAsync();

        // Assert
        Assert.True(response.Headers.Contains("X-Frame-Options"),
            "X-Frame-Options header must be present");
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").First());
    }

    // ── X-Content-Type-Options ────────────────────────────────────────────────────

    [Fact]
    public async Task Response_ContainsXContentTypeOptions_NosniffHeader()
    {
        // Act
        var response = await GetResponseAsync();

        // Assert
        Assert.True(response.Headers.Contains("X-Content-Type-Options"),
            "X-Content-Type-Options header must be present");
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").First());
    }

    // ── X-XSS-Protection ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Response_ContainsXXssProtectionHeader()
    {
        // Act
        var response = await GetResponseAsync();

        // Assert
        Assert.True(response.Headers.Contains("X-XSS-Protection"),
            "X-XSS-Protection header must be present");
        Assert.Equal("1; mode=block", response.Headers.GetValues("X-XSS-Protection").First());
    }

    // ── Content-Security-Policy ───────────────────────────────────────────────────

    [Fact]
    public async Task Response_ContainsContentSecurityPolicyHeader()
    {
        // Act
        var response = await GetResponseAsync();

        // Assert
        Assert.True(response.Headers.Contains("Content-Security-Policy"),
            "Content-Security-Policy header must be present");
        var csp = response.Headers.GetValues("Content-Security-Policy").First();
        Assert.Contains("default-src", csp);
    }

    // ── Referrer-Policy ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Response_ContainsReferrerPolicyHeader()
    {
        // Act
        var response = await GetResponseAsync();

        // Assert
        Assert.True(response.Headers.Contains("Referrer-Policy"),
            "Referrer-Policy header must be present");
        Assert.Equal("strict-origin-when-cross-origin",
            response.Headers.GetValues("Referrer-Policy").First());
    }

    // ── Permissions-Policy ────────────────────────────────────────────────────────

    [Fact]
    public async Task Response_ContainsPermissionsPolicyHeader()
    {
        // Act
        var response = await GetResponseAsync();

        // Assert
        Assert.True(response.Headers.Contains("Permissions-Policy"),
            "Permissions-Policy header must be present");
    }

    // ── All security headers present on health endpoint ───────────────────────────

    [Fact]
    public async Task HealthEndpoint_ContainsAllSecurityHeaders()
    {
        // Act
        var response = await GetResponseAsync("/health");

        // Assert – every mandatory header must be present on this endpoint too
        Assert.True(response.Headers.Contains("X-Frame-Options"));
        Assert.True(response.Headers.Contains("X-Content-Type-Options"));
        Assert.True(response.Headers.Contains("X-XSS-Protection"));
        Assert.True(response.Headers.Contains("Content-Security-Policy"));
        Assert.True(response.Headers.Contains("Referrer-Policy"));
    }
}
