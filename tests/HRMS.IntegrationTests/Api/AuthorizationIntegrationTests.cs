using System.Net;

namespace HRMS.IntegrationTests.Api;

/// <summary>
/// Integration tests that verify all protected MVC routes redirect an
/// unauthenticated user to the login page rather than serving the resource.
/// This exercises the ASP.NET Core authentication middleware end-to-end.
/// </summary>
[Collection(HrmsIntegrationCollection.Name)]
public class AuthorizationIntegrationTests
{
    private readonly HttpClient _client;

    public AuthorizationIntegrationTests(HrmsWebApplicationFactory factory)
    {
        // Disable auto-redirect so we can inspect the 302 Location header.
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    // ── Helper ────────────────────────────────────────────────────────────────────

    private static bool IsAuthRedirect(HttpResponseMessage response)
        => response.StatusCode == HttpStatusCode.Redirect &&
           (response.Headers.Location?.ToString().Contains("/Identity/Account/Login") == true ||
            response.Headers.Location?.ToString().Contains("/Account/Login") == true);

    // ── Employee routes ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetEmployee_Index_WhenUnauthenticated_RedirectsToLogin()
    {
        // Act
        var response = await _client.GetAsync("/Employee");

        // Assert
        Assert.True(IsAuthRedirect(response),
            $"Expected redirect to login page, got {response.StatusCode} → {response.Headers.Location}");
    }

    [Fact]
    public async Task GetEmployee_Create_WhenUnauthenticated_RedirectsToLogin()
    {
        var response = await _client.GetAsync("/Employee/Create");
        Assert.True(IsAuthRedirect(response));
    }

    [Fact]
    public async Task GetEmployee_Details_WhenUnauthenticated_RedirectsToLogin()
    {
        var response = await _client.GetAsync("/Employee/Details/1");
        Assert.True(IsAuthRedirect(response));
    }

    // ── Department routes ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetDepartment_Index_WhenUnauthenticated_RedirectsToLogin()
    {
        var response = await _client.GetAsync("/Department");
        Assert.True(IsAuthRedirect(response));
    }

    [Fact]
    public async Task GetDepartment_Create_WhenUnauthenticated_RedirectsToLogin()
    {
        var response = await _client.GetAsync("/Department/Create");
        Assert.True(IsAuthRedirect(response));
    }

    // ── Dashboard route ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetDashboard_Index_WhenUnauthenticated_RedirectsToLogin()
    {
        var response = await _client.GetAsync("/Dashboard");
        Assert.True(IsAuthRedirect(response));
    }

    // ── Default route ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetRoot_WhenUnauthenticated_RedirectsToLogin()
    {
        var response = await _client.GetAsync("/");
        Assert.True(IsAuthRedirect(response));
    }

    // ── Health endpoints are publicly accessible ──────────────────────────────────

    [Fact]
    public async Task GetHealthLive_WhenUnauthenticated_Returns200()
    {
        // Health probe endpoints must be accessible without authentication.
        var response = await _client.GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
