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

// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Identity
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
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

// Add Memory Cache
builder.Services.AddMemoryCache();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

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

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

app.Run();