using System.Net;

namespace HRMS.IntegrationTests.Security;

/// <summary>
/// Security-focused integration tests that validate the application's defensive
/// behaviours: authentication enforcement, security headers, input handling, and
/// protection against common web attack vectors.
/// </summary>
[Collection(HrmsIntegrationCollection.Name)]
public class SecurityIntegrationTests
{
    private readonly HttpClient _clientNoRedirect;
    private readonly HttpClient _clientWithRedirect;

    public SecurityIntegrationTests(HrmsWebApplicationFactory factory)
    {
        _clientNoRedirect = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        _clientWithRedirect = factory.CreateClient();
    }

    // ── Authentication enforcement ────────────────────────────────────────────────

    [Theory]
    [InlineData("/Employee")]
    [InlineData("/Employee/Create")]
    [InlineData("/Employee/Edit/1")]
    [InlineData("/Department")]
    [InlineData("/Department/Create")]
    [InlineData("/Dashboard")]
    public async Task ProtectedRoute_WhenUnauthenticated_RedirectsToLogin(string route)
    {
        // Act
        var response = await _clientNoRedirect.GetAsync(route);

        // Assert – must be a redirect (not 200 or 401/403)
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location?.ToString() ?? string.Empty;
        Assert.True(
            location.Contains("/Identity/Account/Login") || location.Contains("/Account/Login"),
            $"Route '{route}' did not redirect to login. Location: {location}");
    }

    // ── CSRF / POST protection ─────────────────────────────────────────────────────

    [Fact]
    public async Task PostEmployee_WithoutAntiForgeryToken_RedirectsToLogin()
    {
        // Unauthenticated POST must redirect to login, not reveal a 400 form error
        var response = await _clientNoRedirect.PostAsync("/Employee/Create",
            new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("FirstName", "Hacker"),
                new KeyValuePair<string, string>("LastName", "Attempt")
            }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    [Fact]
    public async Task PostDepartment_WithoutAntiForgeryToken_RedirectsToLogin()
    {
        var response = await _clientNoRedirect.PostAsync("/Department/Create",
            new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Code", "XSS"),
                new KeyValuePair<string, string>("Name", "<script>alert(1)</script>")
            }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    // ── Security headers present on all responses ─────────────────────────────────

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health")]
    public async Task PublicEndpoints_AlwaysIncludeSecurityHeaders(string path)
    {
        // Act
        var response = await _clientNoRedirect.GetAsync(path);

        // Assert
        Assert.True(response.Headers.Contains("X-Frame-Options"),
            $"X-Frame-Options missing on {path}");
        Assert.True(response.Headers.Contains("X-Content-Type-Options"),
            $"X-Content-Type-Options missing on {path}");
        Assert.True(response.Headers.Contains("X-XSS-Protection"),
            $"X-XSS-Protection missing on {path}");
        Assert.True(response.Headers.Contains("Content-Security-Policy"),
            $"Content-Security-Policy missing on {path}");
    }

    // ── XSS payload in query string doesn't cause 5xx ────────────────────────────

    [Theory]
    [InlineData("/Employee?searchTerm=<script>alert(1)</script>")]
    [InlineData("/Employee?searchTerm=%22%3E%3Cscript%3Ealert(1)%3C/script%3E")]
    public async Task XssPayloadInQueryString_DoesNotCauseServerError(string url)
    {
        // The unauthenticated request is redirected to login – what matters is that
        // the server does not throw a 500 when receiving an XSS payload.
        var response = await _clientNoRedirect.GetAsync(url);

        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    // ── SQL injection payload in query string doesn't cause 5xx ──────────────────

    [Theory]
    [InlineData("/Employee?searchTerm='; DROP TABLE Employees;--")]
    [InlineData("/Employee?searchTerm=1 OR 1=1")]
    public async Task SqlInjectionPayloadInQueryString_DoesNotCauseServerError(string url)
    {
        var response = await _clientNoRedirect.GetAsync(url);

        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    // ── Directory traversal doesn't expose files ──────────────────────────────────

    [Theory]
    [InlineData("/../appsettings.json")]
    [InlineData("/..%2F..%2Fappsettings.json")]
    [InlineData("/Employee/Details/../../../etc/passwd")]
    public async Task DirectoryTraversalAttempt_DoesNotReturn200WithSensitiveContent(string path)
    {
        var response = await _clientNoRedirect.GetAsync(path);

        // Must never return 200 for traversal paths; a 404, redirect, or 400 is acceptable
        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
    }

    // ── HTTP methods on protected endpoints ───────────────────────────────────────

    [Fact]
    public async Task DeleteMethodOnEmployeeEndpoint_WhenUnauthenticated_RedirectsToLogin()
    {
        // DELETE (post-only) action on Employee redirects unauthenticated users
        var response = await _clientNoRedirect.DeleteAsync("/Employee");
        // Should either redirect to login or return 405 — but NOT 200 (serving the resource)
        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
    }

    // ── Correlation ID header is propagated ───────────────────────────────────────

    [Fact]
    public async Task Request_WithCorrelationIdHeader_IsAccepted()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add("X-Correlation-ID", Guid.NewGuid().ToString());

        // Act
        var response = await _clientNoRedirect.SendAsync(request);

        // Assert – the server must not reject requests that carry a correlation header
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── Large payloads are handled gracefully ─────────────────────────────────────

    [Fact]
    public async Task ExcessivelyLargeQueryString_DoesNotCauseUnhandledServerError()
    {
        var longValue = new string('A', 10_000);
        var response = await _clientNoRedirect.GetAsync($"/Employee?searchTerm={longValue}");

        // Should redirect to login, not throw a 500
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }
}
