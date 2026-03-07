using HRMS.Core.Entities;
using HRMS.Infrastructure.Data;
using HRMS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HRMS.IntegrationTests.Database;

/// <summary>
/// Integration tests for <see cref="DepartmentRepository"/> using an EF Core
/// in-memory database. These tests verify that CRUD operations and domain-specific
/// query methods work correctly end-to-end through the repository layer.
/// </summary>
public class DepartmentRepositoryIntegrationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly DepartmentRepository _sut;

    public DepartmentRepositoryIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"DeptRepoTests_{Guid.NewGuid()}")
            .Options;

        _context = new ApplicationDbContext(options);
        _sut = new DepartmentRepository(_context);
    }

    public void Dispose() => _context.Dispose();

    // ── Arrange helpers ───────────────────────────────────────────────────────────

    private static Department BuildDepartment(string code = "ENG", string name = "Engineering")
        => new Department { Code = code, Name = name };

    // ── AddAsync / GetByIdAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_ThenGetById_ReturnsSavedDepartment()
    {
        // Arrange
        var dept = BuildDepartment("HR", "Human Resources");

        // Act
        await _sut.AddAsync(dept);
        await _context.SaveChangesAsync();
        var found = await _sut.GetByIdAsync(dept.Id);

        // Assert
        Assert.NotNull(found);
        Assert.Equal("HR", found.Code);
        Assert.Equal("Human Resources", found.Name);
    }

    // ── GetAllAsync ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsAllNonDeletedDepartments()
    {
        // Arrange
        await _sut.AddAsync(BuildDepartment("FIN", "Finance"));
        await _sut.AddAsync(BuildDepartment("MKT", "Marketing"));
        var deleted = BuildDepartment("DEL", "Deleted");
        deleted.IsDeleted = true;
        await _sut.AddAsync(deleted);
        await _context.SaveChangesAsync();

        // Act
        var all = (await _sut.GetAllAsync()).ToList();

        // Assert – soft-deleted departments must be excluded by the global query filter
        Assert.Equal(2, all.Count);
        Assert.DoesNotContain(all, d => d.Code == "DEL");
    }

    // ── Update ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_PersistsChanges()
    {
        // Arrange
        var dept = BuildDepartment("OPS", "Operations");
        await _sut.AddAsync(dept);
        await _context.SaveChangesAsync();

        // Act
        dept.Name = "Operations & Logistics";
        _sut.Update(dept);
        await _context.SaveChangesAsync();
        var updated = await _sut.GetByIdAsync(dept.Id);

        // Assert
        Assert.Equal("Operations & Logistics", updated!.Name);
    }

    // ── Remove (soft-delete) ──────────────────────────────────────────────────────

    [Fact]
    public async Task Remove_SoftDeletesDepartment()
    {
        // Arrange
        var dept = BuildDepartment("LEG", "Legal");
        await _sut.AddAsync(dept);
        await _context.SaveChangesAsync();

        // Act – soft-delete sets IsDeleted = true via ApplicationDbContext.SaveChangesAsync
        _sut.Remove(dept);
        await _context.SaveChangesAsync();

        // Assert – the entity must not appear in filtered queries (global query filter)
        var all = (await _sut.GetAllAsync()).ToList();
        Assert.DoesNotContain(all, d => d.Code == "LEG");

        // Also verify the IsDeleted flag was set
        _context.ChangeTracker.Clear();
        var raw = await _context.Departments.IgnoreQueryFilters().FirstOrDefaultAsync(d => d.Id == dept.Id);
        Assert.NotNull(raw);
        Assert.True(raw!.IsDeleted);
    }

    // ── IsDepartmentCodeUniqueAsync ───────────────────────────────────────────────

    [Fact]
    public async Task IsDepartmentCodeUniqueAsync_WhenCodeDoesNotExist_ReturnsTrue()
    {
        // Act
        var isUnique = await _sut.IsDepartmentCodeUniqueAsync("NEWCODE");

        // Assert
        Assert.True(isUnique);
    }

    [Fact]
    public async Task IsDepartmentCodeUniqueAsync_WhenCodeAlreadyExists_ReturnsFalse()
    {
        // Arrange
        await _sut.AddAsync(BuildDepartment("DUPE", "Duplicate"));
        await _context.SaveChangesAsync();

        // Act
        var isUnique = await _sut.IsDepartmentCodeUniqueAsync("DUPE");

        // Assert
        Assert.False(isUnique);
    }

    [Fact]
    public async Task IsDepartmentCodeUniqueAsync_WhenExcludeIdMatches_ReturnsTrue()
    {
        // Arrange – code belongs to the same department being updated
        var dept = BuildDepartment("EXCL", "Excluded");
        await _sut.AddAsync(dept);
        await _context.SaveChangesAsync();

        // Act
        var isUnique = await _sut.IsDepartmentCodeUniqueAsync("EXCL", excludeId: dept.Id);

        // Assert – the same record should be excluded from the uniqueness check
        Assert.True(isUnique);
    }

    // ── GetDepartmentWithEmployeesAsync ───────────────────────────────────────────

    [Fact]
    public async Task GetDepartmentWithEmployeesAsync_IncludesEmployees()
    {
        // Arrange
        var dept = BuildDepartment("IT", "Information Technology");
        await _sut.AddAsync(dept);
        await _context.SaveChangesAsync();

        var employee = new Employee
        {
            EmployeeCode = "EMP001",
            FirstName = "Alice",
            LastName = "Smith",
            Email = "alice@test.com",
            JobTitle = "Engineer",
            DepartmentId = dept.Id,
            HireDate = DateTime.Today,
            Salary = 60_000
        };
        await _context.Employees.AddAsync(employee);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetDepartmentWithEmployeesAsync(dept.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Employees);
        Assert.Equal("EMP001", result.Employees.First().EmployeeCode);
    }

    // ── GetDepartmentsWithEmployeeCountAsync ──────────────────────────────────────

    [Fact]
    public async Task GetDepartmentsWithEmployeeCountAsync_ReturnsAllDepartments()
    {
        // Arrange
        var dept1 = BuildDepartment("D1", "Dept One");
        var dept2 = BuildDepartment("D2", "Dept Two");
        await _sut.AddAsync(dept1);
        await _sut.AddAsync(dept2);
        await _context.SaveChangesAsync();

        // Act
        var result = (await _sut.GetDepartmentsWithEmployeeCountAsync()).ToList();

        // Assert
        Assert.Equal(2, result.Count);
    }
}
