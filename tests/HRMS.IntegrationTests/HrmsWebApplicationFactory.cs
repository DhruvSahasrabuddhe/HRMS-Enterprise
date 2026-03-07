using HRMS.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace HRMS.IntegrationTests;

/// <summary>
/// Custom <see cref="WebApplicationFactory{TProgram}"/> that replaces the SQL Server
/// database with an in-memory EF Core provider so integration tests run without an
/// external database dependency.
/// </summary>
public class HrmsWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"HrmsIntegrationTest_{Guid.NewGuid()}";

    // All-zero 32-byte AES-256 key and 16-byte IV — test-only, never used in production.
    private static readonly string TestEncryptionKey = Convert.ToBase64String(new byte[32]);
    private static readonly string TestEncryptionIv  = Convert.ToBase64String(new byte[16]);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Provide test encryption keys so EncryptionService initialises without user-secrets.
        builder.ConfigureAppConfiguration(config =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Encryption:Key"] = TestEncryptionKey,
                ["Encryption:IV"]  = TestEncryptionIv
            }));

        builder.ConfigureServices(services =>
        {
            // Remove the existing ApplicationDbContext registration.
            services.RemoveAll(typeof(DbContextOptions<ApplicationDbContext>));

            // Register an in-memory database for the test run.
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        // Ensure the in-memory database schema is created after the host is built,
        // avoiding the service-provider-build-in-ConfigureServices anti-pattern that
        // triggers Serilog's "already frozen" error.
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.EnsureCreated();

        return host;
    }
}
