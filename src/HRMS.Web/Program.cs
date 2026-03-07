using FluentValidation;
using FluentValidation.AspNetCore;
using HRMS.Core.CQRS;
using HRMS.Core.Interfaces.Repositories;
using HRMS.Core.Interfaces.Services;
using HRMS.Infrastructure.Data;
using HRMS.Infrastructure.Repositories;
using HRMS.Infrastructure.Services;
using HRMS.Services.Dashboard;
using HRMS.Services.Departments;
using HRMS.Services.Employees;
using HRMS.Services.Employees.Commands;
using HRMS.Services.Employees.Dtos;
using HRMS.Services.Employees.Handlers;
using HRMS.Services.Employees.Queries;
using HRMS.Services.Leave;
using HRMS.Services.Mappings;
using HRMS.Services.Validators;
using HRMS.Shared.Constants;
using HRMS.Web.Middleware;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Security headers middleware (should be one of the first middleware)
app.UseSecurityHeaders();

// Request logging middleware
app.UseRequestLogging();

app.UseHttpsRedirection();
app.UseStaticFiles(); // This is CRITICAL for CSS/JS to load

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Default route to Dashboard
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.MapRazorPages();

// Ensure database is created and seeded
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