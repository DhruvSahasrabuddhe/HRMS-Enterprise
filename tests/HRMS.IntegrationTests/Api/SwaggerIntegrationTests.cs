using System.Net;
using System.Text.Json;

namespace HRMS.IntegrationTests.Api;

/// <summary>
/// Integration tests that verify the Swagger / OpenAPI documentation endpoint
/// is correctly configured and returns a valid OpenAPI document.
/// </summary>
[Collection(HrmsIntegrationCollection.Name)]
public class SwaggerIntegrationTests
{
    private readonly HttpClient _client;

    public SwaggerIntegrationTests(HrmsWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task GetSwaggerJson_Returns200OK()
    {
        // Act
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetSwaggerJson_ContentType_IsApplicationJson()
    {
        // Act
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        Assert.NotNull(response.Content.Headers.ContentType);
        Assert.Contains("application/json", response.Content.Headers.ContentType.MediaType);
    }

    [Fact]
    public async Task GetSwaggerJson_ContainsApiTitle()
    {
        // Act
        var response = await _client.GetAsync("/swagger/v1/swagger.json");
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        using var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.TryGetProperty("info", out var info));
        Assert.True(info.TryGetProperty("title", out var title));
        Assert.Equal("HRMS Enterprise API", title.GetString());
    }

    [Fact]
    public async Task GetSwaggerJson_ContainsEmployeesPath()
    {
        // Act
        var response = await _client.GetAsync("/swagger/v1/swagger.json");
        var body = await response.Content.ReadAsStringAsync();

        // Assert – the Employees controller must be documented.
        using var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.TryGetProperty("paths", out var paths),
            "OpenAPI document must have a 'paths' property");

        var pathsJson = paths.ToString();
        Assert.Contains("/api/v1/employees", pathsJson,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetSwaggerJson_ContainsDepartmentsPath()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");
        var body = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(body);
        doc.RootElement.TryGetProperty("paths", out var paths);
        Assert.Contains("/api/v1/departments", paths.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetSwaggerJson_ContainsLeavePath()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");
        var body = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(body);
        doc.RootElement.TryGetProperty("paths", out var paths);
        Assert.Contains("/api/v1/leave", paths.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetSwaggerJson_ContainsPayrollPath()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");
        var body = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(body);
        doc.RootElement.TryGetProperty("paths", out var paths);
        Assert.Contains("/api/v1/payroll", paths.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetSwaggerUi_Returns200OK()
    {
        // Act
        var response = await _client.GetAsync("/swagger/index.html");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetApiEndpoint_WithoutAuth_ReturnsJsonErrorNotHtml()
    {
        // API endpoints should return JSON error responses (not HTML redirect pages)
        // for unauthenticated requests.
        var response = await _client.GetAsync("/api/v1/employees");

        // Assert status is 401
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        // The response should not be an HTML redirect page.
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
        Assert.DoesNotContain("text/html", contentType, StringComparison.OrdinalIgnoreCase);
    }
}
