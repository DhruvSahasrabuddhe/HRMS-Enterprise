using AutoMapper;
using FluentValidation;
using FluentValidation.Results;
using HRMS.Core.Entities;
using HRMS.Core.Enums;
using HRMS.Core.Interfaces.Repositories;
using HRMS.Services.Employees;
using HRMS.Services.Employees.Dtos;
using HRMS.Services.Mappings;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace HRMS.UnitTests.Services
{
    public class EmployeeServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IEmployeeRepository> _employeeRepoMock;
        private readonly Mock<IDepartmentRepository> _departmentRepoMock;
        private readonly Mock<ILeaveRepository> _leaveRepoMock;
        private readonly Mock<IAttendanceRepository> _attendanceRepoMock;
        private readonly IMapper _mapper;
        private readonly Mock<ILogger<EmployeeService>> _loggerMock;
        private readonly IMemoryCache _cache;
        private readonly Mock<IValidator<CreateEmployeeDto>> _createValidatorMock;
        private readonly Mock<IValidator<UpdateEmployeeDto>> _updateValidatorMock;
        private readonly EmployeeService _sut;

        public EmployeeServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _employeeRepoMock = new Mock<IEmployeeRepository>();
            _departmentRepoMock = new Mock<IDepartmentRepository>();
            _leaveRepoMock = new Mock<ILeaveRepository>();
            _attendanceRepoMock = new Mock<IAttendanceRepository>();
            _loggerMock = new Mock<ILogger<EmployeeService>>();
            _createValidatorMock = new Mock<IValidator<CreateEmployeeDto>>();
            _updateValidatorMock = new Mock<IValidator<UpdateEmployeeDto>>();

            _unitOfWorkMock.Setup(u => u.Employees).Returns(_employeeRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.Departments).Returns(_departmentRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.Leaves).Returns(_leaveRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.Attendances).Returns(_attendanceRepoMock.Object);

            var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
            _mapper = config.CreateMapper();

            var opts = new MemoryCacheOptions();
            _cache = new MemoryCache(opts);

            _sut = new EmployeeService(
                _unitOfWorkMock.Object,
                _mapper,
                _loggerMock.Object,
                _cache,
                _createValidatorMock.Object,
                _updateValidatorMock.Object);
        }

        [Fact]
        public async Task GetEmployeeByIdAsync_WhenEmployeeExists_ReturnsDto()
        {
            // Arrange
            var department = new Department { Id = 1, Name = "Engineering", Code = "ENG" };
            var employee = new Employee
            {
                Id = 1,
                EmployeeCode = "EMP00001",
                FirstName = "Alice",
                LastName = "Smith",
                Email = "alice@example.com",
                JobTitle = "Developer",
                DepartmentId = 1,
                Department = department,
                Status = EmployeeStatus.Active
            };

            _employeeRepoMock
                .Setup(r => r.GetEmployeeWithDetailsAsync(1))
                .ReturnsAsync(employee);

            // Act
            var result = await _sut.GetEmployeeByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("Alice", result.FirstName);
            Assert.Equal("Smith", result.LastName);
            Assert.Equal("Engineering", result.DepartmentName);
        }

        [Fact]
        public async Task GetEmployeeByIdAsync_WhenEmployeeDoesNotExist_ReturnsNull()
        {
            _employeeRepoMock
                .Setup(r => r.GetEmployeeWithDetailsAsync(999))
                .ReturnsAsync((Employee?)null);

            var result = await _sut.GetEmployeeByIdAsync(999);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetEmployeeByIdAsync_SecondCall_ReturnsCachedResult()
        {
            // Arrange
            var department = new Department { Id = 1, Name = "HR", Code = "HR" };
            var employee = new Employee
            {
                Id = 5,
                EmployeeCode = "EMP00005",
                FirstName = "Bob",
                LastName = "Jones",
                Email = "bob@example.com",
                JobTitle = "Manager",
                DepartmentId = 1,
                Department = department,
                Status = EmployeeStatus.Active
            };

            _employeeRepoMock
                .Setup(r => r.GetEmployeeWithDetailsAsync(5))
                .ReturnsAsync(employee);

            // Act – two calls
            await _sut.GetEmployeeByIdAsync(5);
            await _sut.GetEmployeeByIdAsync(5);

            // Assert – repository called only once (second call uses cache)
            _employeeRepoMock.Verify(r => r.GetEmployeeWithDetailsAsync(5), Times.Once);
        }

        [Fact]
        public async Task GetAllEmployeesAsync_ReturnsAllEmployees()
        {
            // Arrange
            var department = new Department { Id = 1, Name = "Engineering", Code = "ENG" };
            var employees = new List<Employee>
            {
                new() { Id = 1, FirstName = "Alice", LastName = "A", Email = "a@x.com", JobTitle = "Dev",
                        EmployeeCode = "EMP00001", DepartmentId = 1, Department = department },
                new() { Id = 2, FirstName = "Bob",   LastName = "B", Email = "b@x.com", JobTitle = "QA",
                        EmployeeCode = "EMP00002", DepartmentId = 1, Department = department }
            };

            _employeeRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(employees);

            // Act
            var result = await _sut.GetAllEmployeesAsync();

            // Assert
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task CreateEmployeeAsync_WithValidData_CreatesAndReturnsEmployee()
        {
            // Arrange
            var createDto = new CreateEmployeeDto
            {
                FirstName = "Carol",
                LastName = "White",
                Email = "carol@example.com",
                JobTitle = "Analyst",
                DepartmentId = 1,
                DateOfBirth = new DateTime(1990, 1, 1),
                HireDate = DateTime.Today,
                Salary = 5000m
            };

            _createValidatorMock
                .Setup(v => v.ValidateAsync(createDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _employeeRepoMock
                .Setup(r => r.IsEmailUniqueAsync(createDto.Email, It.IsAny<int?>()))
                .ReturnsAsync(true);

            _employeeRepoMock
                .Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Employee, bool>>>()))
                .ReturnsAsync(0);

            _employeeRepoMock
                .Setup(r => r.AddAsync(It.IsAny<Employee>()))
                .Returns(Task.CompletedTask);

            _unitOfWorkMock
                .Setup(u => u.CompleteAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _sut.CreateEmployeeAsync(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Carol", result.FirstName);
            Assert.Equal("White", result.LastName);
            _employeeRepoMock.Verify(r => r.AddAsync(It.IsAny<Employee>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateEmployeeAsync_WithValidationErrors_ThrowsValidationException()
        {
            // Arrange
            var createDto = new CreateEmployeeDto { FirstName = "", LastName = "", Email = "invalid" };

            var failures = new List<ValidationFailure>
            {
                new("FirstName", "First name is required"),
                new("Email", "Invalid email format")
            };

            _createValidatorMock
                .Setup(v => v.ValidateAsync(createDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(failures));

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(() => _sut.CreateEmployeeAsync(createDto));
        }

        [Fact]
        public async Task CreateEmployeeAsync_WithDuplicateEmail_ThrowsInvalidOperationException()
        {
            // Arrange
            var createDto = new CreateEmployeeDto
            {
                FirstName = "Dave",
                LastName = "Green",
                Email = "existing@example.com",
                JobTitle = "Dev",
                DepartmentId = 1,
                DateOfBirth = new DateTime(1985, 5, 15),
                HireDate = DateTime.Today
            };

            _createValidatorMock
                .Setup(v => v.ValidateAsync(createDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _employeeRepoMock
                .Setup(r => r.IsEmailUniqueAsync(createDto.Email, It.IsAny<int?>()))
                .ReturnsAsync(false);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateEmployeeAsync(createDto));
        }

        [Fact]
        public async Task DeleteEmployeeAsync_WhenEmployeeExists_DeletesEmployee()
        {
            // Arrange
            var department = new Department { Id = 1, Name = "IT", Code = "IT" };
            var employee = new Employee
            {
                Id = 7,
                FirstName = "Eve",
                LastName = "Brown",
                Email = "eve@example.com",
                JobTitle = "Tester",
                EmployeeCode = "EMP00007",
                DepartmentId = 1,
                Department = department
            };

            _employeeRepoMock.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(employee);
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            // Act
            var result = await _sut.DeleteEmployeeAsync(7);

            // Assert
            Assert.True(result);
            Assert.True(employee.IsDeleted);
            Assert.Equal(EmployeeStatus.Terminated, employee.Status);
            _employeeRepoMock.Verify(r => r.Update(employee), Times.Once);
            _unitOfWorkMock.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteEmployeeAsync_WhenEmployeeNotFound_ThrowsKeyNotFoundException()
        {
            _employeeRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Employee?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.DeleteEmployeeAsync(999));
        }
    }
}
