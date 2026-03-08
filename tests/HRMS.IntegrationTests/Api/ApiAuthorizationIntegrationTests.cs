using System.Net;

namespace HRMS.IntegrationTests.Api;

/// <summary>
/// Integration tests that verify all REST API v1 endpoints return HTTP 401
/// (Unauthorized) rather than redirecting when called without authentication.
/// API controllers use <c>[ApiController]</c> which produces a 401 JSON response
/// instead of the cookie-redirect that MVC controllers issue.
/// </summary>
[Collection(HrmsIntegrationCollection.Name)]
public class ApiAuthorizationIntegrationTests
{
    private readonly HttpClient _client;

    public ApiAuthorizationIntegrationTests(HrmsWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    // ── Employees ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetEmployees_WhenUnauthenticated_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/employees");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetEmployee_WhenUnauthenticated_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/employees/1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostEmployee_WhenUnauthenticated_Returns401()
    {
        var response = await _client.PostAsync("/api/v1/employees",
            new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PutEmployee_WhenUnauthenticated_Returns401()
    {
        var response = await _client.PutAsync("/api/v1/employees/1",
            new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEmployee_WhenUnauthenticated_Returns401()
    {
        var response = await _client.DeleteAsync("/api/v1/employees/1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Departments ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetDepartments_WhenUnauthenticated_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/departments");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetDepartment_WhenUnauthenticated_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/departments/1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostDepartment_WhenUnauthenticated_Returns401()
    {
        var response = await _client.PostAsync("/api/v1/departments",
            new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteDepartment_WhenUnauthenticated_Returns401()
    {
        var response = await _client.DeleteAsync("/api/v1/departments/1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Leave ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetLeaveRequests_WhenUnauthenticated_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/leave");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetLeaveRequest_WhenUnauthenticated_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/leave/1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostLeaveRequest_WhenUnauthenticated_Returns401()
    {
        var response = await _client.PostAsync("/api/v1/leave",
            new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostApproveLeave_WhenUnauthenticated_Returns401()
    {
        var response = await _client.PostAsync("/api/v1/leave/1/approve",
            new StringContent("{\"id\":1}", System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostRejectLeave_WhenUnauthenticated_Returns401()
    {
        var response = await _client.PostAsync("/api/v1/leave/1/reject",
            new StringContent("{\"id\":1}", System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Attendance ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAttendance_WhenUnauthenticated_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/attendance/1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostAttendanceCheckIn_WhenUnauthenticated_Returns401()
    {
        var response = await _client.PostAsync("/api/v1/attendance/check-in",
            new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostAttendanceCheckOut_WhenUnauthenticated_Returns401()
    {
        var response = await _client.PostAsync("/api/v1/attendance/check-out",
            new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Payroll ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPayroll_WhenUnauthenticated_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/payroll/1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostPayrollProcess_WhenUnauthenticated_Returns401()
    {
        var response = await _client.PostAsync("/api/v1/payroll/process",
            new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostPayrollApprove_WhenUnauthenticated_Returns401()
    {
        var response = await _client.PostAsync("/api/v1/payroll/1/approve",
            new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Performance Reviews ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetPerformanceReview_WhenUnauthenticated_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/performance-reviews/1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostPerformanceReview_WhenUnauthenticated_Returns401()
    {
        var response = await _client.PostAsync("/api/v1/performance-reviews",
            new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostFinalizeReview_WhenUnauthenticated_Returns401()
    {
        var response = await _client.PostAsync("/api/v1/performance-reviews/1/finalize",
            new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
