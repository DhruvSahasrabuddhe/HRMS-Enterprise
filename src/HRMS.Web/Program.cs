using FluentValidation;
using FluentValidation.AspNetCore;
using HRMS.Core.CQRS;
using HRMS.Core.Interfaces.Repositories;
using HRMS.Core.Interfaces.Services;
using HRMS.Infrastructure.Data;
using HRMS.Infrastructure.Repositories;
using HRMS.Infrastructure.Services;
using HRMS.Services.Attendance;
using HRMS.Services.Dashboard;
using HRMS.Services.Departments;
using HRMS.Services.Employees;
using HRMS.Services.Employees.Commands;
using HRMS.Services.Employees.Dtos;
using HRMS.Services.Employees.Handlers;
using HRMS.Services.Employees.Queries;
using HRMS.Services.Leave;
using HRMS.Services.Mappings;
using HRMS.Services.Payroll;
using HRMS.Services.PerformanceReviews;
using HRMS.Services.Reports;
using HRMS.Services.Validators;
using HRMS.Shared.Constants;
using HRMS.Web.HealthChecks;
using HRMS.Web.Middleware;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Reflection;
using System.Text.Json;

// ─── Configure Serilog before the host is built so startup errors are captured ───
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ─── Serilog: structured JSON logging read from appsettings ─────────────────
    builder.Host.UseSerilog((context, services, configuration) =>
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName());

    // Add services to the container.
    builder.Services.AddControllersWithViews();
    builder.Services.AddRazorPages();

    // ─── OpenAPI / Swagger ────────────────────────────────────────────────────────
    // Swashbuckle is already included in the project.  We enable it for all
    // environments so that API consumers (including integration tests) can
    // discover the contract at /swagger/v1/swagger.json.
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc(HrmsConstants.Api.CurrentVersion, new OpenApiInfo
        {
            Title = HrmsConstants.Api.Title,
            Version = HrmsConstants.Api.CurrentVersion,
            Description = HrmsConstants.Api.Description + "\n\n" +
                "**Authentication**: All endpoints require a valid authenticated session " +
                "(cookie-based via ASP.NET Core Identity).\n\n" +
                "**Pagination**: Collection responses include `X-Total-Count`, `X-Total-Pages`, " +
                "`X-Current-Page`, and `X-Page-Size` response headers.\n\n" +
                "**Rate Limiting**: Every response includes `X-RateLimit-Limit`, " +
                "`X-RateLimit-Remaining`, and `X-RateLimit-Reset` headers.\n\n" +
                "**Errors**: All error responses follow the RFC 7807 Problem Details format " +
                "with additional `errorCode` and `correlationId` fields.",
            Contact = new OpenApiContact
            {
                Name = "HRMS Support",
                Email = "support@hrms.example.com"
            }
        });

        // Include XML documentation comments from the Web project.
        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath);
        }

        // Document the cookie-based authentication scheme used by Identity.
        options.AddSecurityDefinition("cookieAuth", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Cookie,
            Name = ".AspNetCore.Identity.Application",
            Description = "ASP.NET Core Identity authentication cookie. " +
                          "Log in via /Identity/Account/Login to obtain the session cookie."
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "cookieAuth"
                    }
                },
                Array.Empty<string>()
            }
        });

        // Only expose /api routes in the Swagger document (exclude MVC routes).
        options.DocInclusionPredicate((_, apiDesc) =>
            apiDesc.RelativePath?.StartsWith("api/", StringComparison.OrdinalIgnoreCase) == true);
    });

    // Add HttpContextAccessor (registered early so it can be used by DbContext)
    builder.Services.AddHttpContextAccessor();

    // Add current-user service (resolves identity from HTTP context)
    builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

    // Add DbContext with connection resilience and command-timeout configuration
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: HrmsConstants.Database.MaxRetryCount,
                    maxRetryDelay: TimeSpan.FromSeconds(HrmsConstants.Database.MaxRetryDelaySeconds),
                    errorNumbersToAdd: null);
                sqlOptions.CommandTimeout(HrmsConstants.Database.CommandTimeoutSeconds);
                sqlOptions.MaxBatchSize(HrmsConstants.Database.MaxBatchSize);
            }));

    // Add Identity
    builder.Services.AddDefaultIdentity<IdentityUser>(options =>
    {
        // Password policy configuration
        options.Password.RequiredLength = HrmsConstants.Security.MinPasswordLength;
        options.Password.RequireDigit = HrmsConstants.Security.RequireDigit;
        options.Password.RequireLowercase = HrmsConstants.Security.RequireLowercase;
        options.Password.RequireUppercase = HrmsConstants.Security.RequireUppercase;
        options.Password.RequireNonAlphanumeric = HrmsConstants.Security.RequireNonAlphanumeric;
        options.Password.RequiredUniqueChars = HrmsConstants.Security.RequiredUniqueChars;

        // Account lockout configuration
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(HrmsConstants.Security.LockoutDurationMinutes);
        options.Lockout.MaxFailedAccessAttempts = HrmsConstants.Security.MaxFailedAccessAttempts;
        options.Lockout.AllowedForNewUsers = HrmsConstants.Security.AllowedForNewUsers;

        // Sign-in configuration
        options.SignIn.RequireConfirmedAccount = false;

        // User configuration
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

    // Configure the Identity application cookie so that requests to /api/* receive
    // HTTP 401/403 JSON responses instead of being redirected to the login page.
    // This allows REST clients to handle auth errors programmatically.
    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };

        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });

    // Add Repositories
    builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
    builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
    builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
    builder.Services.AddScoped<ILeaveRepository, LeaveRepository>();
    builder.Services.AddScoped<IAttendanceRepository, AttendanceRepository>();

    // Add infrastructure services
    builder.Services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
    builder.Services.AddScoped<IEmployeeCodeGenerator, EmployeeCodeGenerator>();
    builder.Services.AddScoped<IEncryptionService, EncryptionService>();

    // Add Services
    builder.Services.AddScoped<IEmployeeService, EmployeeService>();
    builder.Services.AddScoped<IDepartmentService, DepartmentService>();
    builder.Services.AddScoped<ILeaveService, LeaveService>();
    builder.Services.AddScoped<IDashboardService, DashboardService>();
    builder.Services.AddScoped<IAttendanceService, AttendanceService>();
    builder.Services.AddScoped<IPerformanceReviewService, PerformanceReviewService>();
    builder.Services.AddScoped<IPayrollService, PayrollService>();
    builder.Services.AddScoped<IReportService, ReportService>();

    // Add Repositories for new entities
    builder.Services.AddScoped<IPerformanceReviewRepository, PerformanceReviewRepository>();
    builder.Services.AddScoped<IPayrollRepository, PayrollRepository>();

    // Register CQRS handlers for Employee domain
    builder.Services.AddScoped<ICommandHandler<CreateEmployeeCommand, EmployeeDto>, CreateEmployeeCommandHandler>();
    builder.Services.AddScoped<ICommandHandler<UpdateEmployeeCommand, EmployeeDto>, UpdateEmployeeCommandHandler>();
    builder.Services.AddScoped<ICommandHandler<DeleteEmployeeCommand, Unit>, DeleteEmployeeCommandHandler>();
    builder.Services.AddScoped<IQueryHandler<GetEmployeeByIdQuery, EmployeeDto?>, GetEmployeeByIdQueryHandler>();
    builder.Services.AddScoped<IQueryHandler<GetAllEmployeesQuery, IEnumerable<EmployeeListDto>>, GetAllEmployeesQueryHandler>();
    builder.Services.AddScoped<IQueryHandler<SearchEmployeesQuery, IEnumerable<EmployeeListDto>>, SearchEmployeesQueryHandler>();

    // Add AutoMapper
    builder.Services.AddAutoMapper(typeof(MappingProfile));

    // Add FluentValidation
    builder.Services.AddFluentValidationAutoValidation();
    builder.Services.AddValidatorsFromAssemblyContaining<CreateEmployeeValidator>();

    // Add Memory Cache (in-process, always available)
    builder.Services.AddMemoryCache();

    // Add Distributed Cache.
    // When a Redis connection string is configured, use Redis; otherwise fall back to the
    // in-process distributed-cache implementation so the application works out-of-the-box.
    var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
    if (!string.IsNullOrWhiteSpace(redisConnectionString))
    {
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "HRMS:";
        });
    }
    else
    {
        builder.Services.AddDistributedMemoryCache();
    }

    // Register the typed distributed-cache abstraction used by application services.
    builder.Services.AddScoped<IDistributedCacheService, DistributedCacheService>();

    // ─── Health Checks ────────────────────────────────────────────────────────────
    builder.Services.AddHealthChecks()
        .AddCheck<DatabaseHealthCheck>(
            HrmsConstants.HealthChecks.DatabaseCheckName,
            tags: [HrmsConstants.HealthChecks.InfrastructureTag])
        .AddCheck<HrmsBusinessHealthCheck>(
            "hrms-business",
            tags: [HrmsConstants.HealthChecks.BusinessTag]);

    var app = builder.Build();

    // ─── Middleware pipeline ──────────────────────────────────────────────────────

    // Global exception handler must be first so it catches errors from all layers.
    app.UseGlobalExceptionHandler();

    // Correlation ID propagation (before logging so log entries carry the ID).
    app.UseCorrelationId();

    // Configure the built-in exception handler page for non-API environments.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    // Security headers middleware
    app.UseSecurityHeaders();

    // Request logging middleware
    app.UseRequestLogging();

    app.UseHttpsRedirection();
    app.UseStaticFiles(); // This is CRITICAL for CSS/JS to load

    // ─── Swagger / OpenAPI ────────────────────────────────────────────────────────
    // Enabled in all environments so API consumers and integration tests can always
    // discover the OpenAPI contract at /swagger/v1/swagger.json.
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            $"/swagger/{HrmsConstants.Api.CurrentVersion}/swagger.json",
            $"{HrmsConstants.Api.Title} {HrmsConstants.Api.CurrentVersion}");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = HrmsConstants.Api.Title;
    });

    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();

    // ─── Health check endpoints ───────────────────────────────────────────────────
    // Detailed endpoint: returns full JSON report including all checks and their data.
    app.MapHealthChecks(HrmsConstants.HealthChecks.DetailedEndpoint, new HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            var result = JsonSerializer.Serialize(new
            {
                status = report.Status.ToString(),
                checks = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString(),
                    description = e.Value.Description,
                    data = e.Value.Data,
                    duration = e.Value.Duration.TotalMilliseconds
                }),
                totalDuration = report.TotalDuration.TotalMilliseconds
            }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            await context.Response.WriteAsync(result);
        }
    });

    // Liveness probe: simple 200/503 with no body – suitable for load-balancer checks.
    app.MapHealthChecks(HrmsConstants.HealthChecks.LiveEndpoint, new HealthCheckOptions
    {
        Predicate = _ => false, // skip all named checks; just confirms the app is alive
        ResultStatusCodes =
        {
            [HealthStatus.Healthy] = StatusCodes.Status200OK,
            [HealthStatus.Degraded] = StatusCodes.Status200OK,
            [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
        }
    });

    // Default route to Dashboard
    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Dashboard}/{action=Index}/{id?}");

    app.MapRazorPages();

    // Ensure database is created and seeded (skip in automated test environments)
    if (!app.Environment.IsEnvironment("Testing"))
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            context.Database.Migrate();

            // Seed roles and admin user
            var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var logger = services.GetRequiredService<ILogger<Program>>();

            await DbInitializer.SeedAsync(userManager, roleManager, logger);
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while migrating or seeding the database.");
        }
    }

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}
// Expose Program class for integration tests
public partial class Program { }
