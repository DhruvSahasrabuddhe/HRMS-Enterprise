using HRMS.Core.Entities;
using HRMS.Core.Enums;
using HRMS.Infrastructure.Data;
using HRMS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HRMS.IntegrationTests.Database;

/// <summary>
/// Integration tests for <see cref="EmployeeRepository"/> using an EF Core
/// in-memory database. Verifies CRUD operations, search, uniqueness checks,
/// paged queries, and relationship loading.
/// </summary>
public class EmployeeRepositoryIntegrationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly EmployeeRepository _sut;

    public EmployeeRepositoryIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"EmpRepoTests_{Guid.NewGuid()}")
            .Options;

        _context = new ApplicationDbContext(options);
        _sut = new EmployeeRepository(_context);
    }

    public void Dispose() => _context.Dispose();

    // ── Arrange helpers ───────────────────────────────────────────────────────────

    private async Task<Department> SeedDepartmentAsync(string code = "ENG", string name = "Engineering")
    {
        var dept = new Department { Code = code, Name = name };
        await _context.Departments.AddAsync(dept);
        await _context.SaveChangesAsync();
        return dept;
    }

    private Employee BuildEmployee(
        string code, string firstName, string email,
        int departmentId, EmployeeStatus status = EmployeeStatus.Active)
        => new Employee
        {
            EmployeeCode = code,
            FirstName = firstName,
            LastName = "Test",
            Email = email,
            JobTitle = "Engineer",
            DepartmentId = departmentId,
            HireDate = new DateTime(2022, 1, 1),
            Status = status,
            Salary = 50_000
        };

    // ── AddAsync / GetByIdAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_ThenGetById_ReturnsSavedEmployee()
    {
        // Arrange
        var dept = await SeedDepartmentAsync();
        var emp = BuildEmployee("EMP001", "Alice", "alice@test.com", dept.Id);

        // Act
        await _sut.AddAsync(emp);
        await _context.SaveChangesAsync();
        var found = await _sut.GetByIdAsync(emp.Id);

        // Assert
        Assert.NotNull(found);
        Assert.Equal("EMP001", found.EmployeeCode);
        Assert.Equal("Alice", found.FirstName);
    }

    // ── GetEmployeeWithDetailsAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetEmployeeWithDetailsAsync_LoadsDepartmentNavigation()
    {
        // Arrange
        var dept = await SeedDepartmentAsync("IT", "Information Technology");
        var emp = BuildEmployee("EMP002", "Bob", "bob@test.com", dept.Id);
        await _sut.AddAsync(emp);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetEmployeeWithDetailsAsync(emp.Id);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Department);
        Assert.Equal("IT", result.Department!.Code);
    }

    // ── GetAllAsync / global query filter ─────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ExcludesSoftDeletedEmployees()
    {
        // Arrange
        var dept = await SeedDepartmentAsync();
        var active = BuildEmployee("ACT01", "Active", "active@test.com", dept.Id);
        var deleted = BuildEmployee("DEL01", "Deleted", "deleted@test.com", dept.Id);
        await _sut.AddAsync(active);
        await _sut.AddAsync(deleted);
        await _context.SaveChangesAsync();

        // Soft-delete by using the tracked entity
        deleted.IsDeleted = true;
        await _context.SaveChangesAsync();

        // Act
        var all = (await _sut.GetAllAsync()).ToList();

        // Assert
        Assert.DoesNotContain(all, e => e.EmployeeCode == "DEL01");
        Assert.Contains(all, e => e.EmployeeCode == "ACT01");
    }

    // ── GetEmployeesByDepartmentAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GetEmployeesByDepartmentAsync_ReturnsOnlyMatchingDepartment()
    {
        // Arrange
        var dept1 = await SeedDepartmentAsync("D1", "Dept 1");
        var dept2 = await SeedDepartmentAsync("D2", "Dept 2");

        await _sut.AddAsync(BuildEmployee("E1", "Emp1", "emp1@test.com", dept1.Id));
        await _sut.AddAsync(BuildEmployee("E2", "Emp2", "emp2@test.com", dept2.Id));
        await _sut.AddAsync(BuildEmployee("E3", "Emp3", "emp3@test.com", dept1.Id));
        await _context.SaveChangesAsync();

        // Act
        var result = (await _sut.GetEmployeesByDepartmentAsync(dept1.Id)).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, e => Assert.Equal(dept1.Id, e.DepartmentId));
    }

    // ── SearchEmployeesAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task SearchEmployeesAsync_ByFirstName_ReturnsMatchingEmployees()
    {
        // Arrange
        var dept = await SeedDepartmentAsync();
        await _sut.AddAsync(BuildEmployee("S001", "Charlie", "charlie@test.com", dept.Id));
        await _sut.AddAsync(BuildEmployee("S002", "Diana", "diana@test.com", dept.Id));
        await _context.SaveChangesAsync();

        // Act
        var results = (await _sut.SearchEmployeesAsync("charlie")).ToList();

        // Assert
        Assert.Single(results);
        Assert.Equal("S001", results[0].EmployeeCode);
    }

    [Fact]
    public async Task SearchEmployeesAsync_ByEmail_ReturnsMatchingEmployee()
    {
        // Arrange
        var dept = await SeedDepartmentAsync();
        await _sut.AddAsync(BuildEmployee("S003", "Eve", "eve.unique@test.com", dept.Id));
        await _sut.AddAsync(BuildEmployee("S004", "Frank", "frank@test.com", dept.Id));
        await _context.SaveChangesAsync();

        // Act
        var results = (await _sut.SearchEmployeesAsync("eve.unique")).ToList();

        // Assert
        Assert.Single(results);
        Assert.Equal("S003", results[0].EmployeeCode);
    }

    // ── IsEmailUniqueAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task IsEmailUniqueAsync_WhenEmailNotUsed_ReturnsTrue()
    {
        // Act
        var isUnique = await _sut.IsEmailUniqueAsync("new@test.com");

        // Assert
        Assert.True(isUnique);
    }

    [Fact]
    public async Task IsEmailUniqueAsync_WhenEmailAlreadyExists_ReturnsFalse()
    {
        // Arrange
        var dept = await SeedDepartmentAsync();
        await _sut.AddAsync(BuildEmployee("U001", "Grace", "grace@test.com", dept.Id));
        await _context.SaveChangesAsync();

        // Act
        var isUnique = await _sut.IsEmailUniqueAsync("grace@test.com");

        // Assert
        Assert.False(isUnique);
    }

    // ── IsEmployeeCodeUniqueAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task IsEmployeeCodeUniqueAsync_WhenCodeNotUsed_ReturnsTrue()
    {
        var isUnique = await _sut.IsEmployeeCodeUniqueAsync("BRAND_NEW");
        Assert.True(isUnique);
    }

    [Fact]
    public async Task IsEmployeeCodeUniqueAsync_WhenCodeExists_ReturnsFalse()
    {
        // Arrange
        var dept = await SeedDepartmentAsync();
        await _sut.AddAsync(BuildEmployee("TAKEN", "Hank", "hank@test.com", dept.Id));
        await _context.SaveChangesAsync();

        // Act
        var isUnique = await _sut.IsEmployeeCodeUniqueAsync("TAKEN");

        // Assert
        Assert.False(isUnique);
    }

    // ── GetEmployeeCountByDepartmentAsync ─────────────────────────────────────────

    [Fact]
    public async Task GetEmployeeCountByDepartmentAsync_ReturnsCorrectCount()
    {
        // Arrange
        var dept = await SeedDepartmentAsync("CNT", "Count Dept");
        await _sut.AddAsync(BuildEmployee("C1", "Irene", "irene@test.com", dept.Id));
        await _sut.AddAsync(BuildEmployee("C2", "Jack", "jack@test.com", dept.Id));
        await _context.SaveChangesAsync();

        // Act
        var count = await _sut.GetEmployeeCountByDepartmentAsync(dept.Id);

        // Assert
        Assert.Equal(2, count);
    }

    // ── SearchEmployeesPagedAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task SearchEmployeesPagedAsync_PaginatesResults()
    {
        // Arrange
        var dept = await SeedDepartmentAsync("PAG", "Paging Dept");
        for (int i = 1; i <= 5; i++)
        {
            await _sut.AddAsync(BuildEmployee($"P{i:D3}", $"Page{i}", $"page{i}@test.com", dept.Id));
        }
        await _context.SaveChangesAsync();

        // Act – request page 1 with page size 2
        var page1 = (await _sut.SearchEmployeesPagedAsync(
            searchTerm: null, departmentId: dept.Id, managerId: null, status: null,
            sortBy: "FirstName", sortAscending: true, pageNumber: 1, pageSize: 2)).ToList();

        // Act – request page 2 with page size 2
        var page2 = (await _sut.SearchEmployeesPagedAsync(
            searchTerm: null, departmentId: dept.Id, managerId: null, status: null,
            sortBy: "FirstName", sortAscending: true, pageNumber: 2, pageSize: 2)).ToList();

        // Assert
        Assert.Equal(2, page1.Count);
        Assert.Equal(2, page2.Count);
        Assert.DoesNotContain(page2, e => page1.Any(p => p.Id == e.Id));
    }

    [Fact]
    public async Task SearchEmployeesPagedAsync_FiltersByStatus()
    {
        // Arrange
        var dept = await SeedDepartmentAsync("STS", "Status Dept");
        await _sut.AddAsync(BuildEmployee("ST1", "Karen", "karen@test.com", dept.Id, EmployeeStatus.Active));
        await _sut.AddAsync(BuildEmployee("ST2", "Leo", "leo@test.com", dept.Id, EmployeeStatus.Inactive));
        await _context.SaveChangesAsync();

        // Act
        var active = (await _sut.SearchEmployeesPagedAsync(
            null, null, null, EmployeeStatus.Active,
            "FirstName", true, 1, 10)).ToList();

        // Assert
        Assert.All(active, e => Assert.Equal(EmployeeStatus.Active, e.Status));
    }

    // ── GetEmployeesHiredBetweenAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GetEmployeesHiredBetweenAsync_ReturnsEmployeesInRange()
    {
        // Arrange
        var dept = await SeedDepartmentAsync("HRG", "Hire Range");

        var early = BuildEmployee("HR1", "Mary", "mary@test.com", dept.Id);
        early.HireDate = new DateTime(2020, 1, 15);

        var inRange = BuildEmployee("HR2", "Nick", "nick@test.com", dept.Id);
        inRange.HireDate = new DateTime(2021, 6, 1);

        var late = BuildEmployee("HR3", "Olivia", "olivia@test.com", dept.Id);
        late.HireDate = new DateTime(2023, 3, 20);

        await _sut.AddAsync(early);
        await _sut.AddAsync(inRange);
        await _sut.AddAsync(late);
        await _context.SaveChangesAsync();

        // Act
        var result = (await _sut.GetEmployeesHiredBetweenAsync(
            new DateTime(2021, 1, 1), new DateTime(2021, 12, 31))).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal("HR2", result[0].EmployeeCode);
    }
}
