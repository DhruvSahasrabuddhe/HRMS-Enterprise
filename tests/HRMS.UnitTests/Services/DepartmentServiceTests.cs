using AutoMapper;
using FluentValidation;
using FluentValidation.Results;
using HRMS.Core.Entities;
using HRMS.Core.Enums;
using HRMS.Core.Interfaces.Repositories;
using HRMS.Services.Departments;
using HRMS.Services.Departments.Dtos;
using HRMS.Services.Mappings;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace HRMS.UnitTests.Services
{
    public class DepartmentServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IDepartmentRepository> _departmentRepoMock;
        private readonly Mock<IEmployeeRepository> _employeeRepoMock;
        private readonly Mock<ILeaveRepository> _leaveRepoMock;
        private readonly Mock<IAttendanceRepository> _attendanceRepoMock;
        private readonly IMapper _mapper;
        private readonly Mock<ILogger<DepartmentService>> _loggerMock;
        private readonly IMemoryCache _cache;
        private readonly Mock<IValidator<CreateDepartmentDto>> _createValidatorMock;
        private readonly Mock<IValidator<UpdateDepartmentDto>> _updateValidatorMock;
        private readonly DepartmentService _sut;

        public DepartmentServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _departmentRepoMock = new Mock<IDepartmentRepository>();
            _employeeRepoMock = new Mock<IEmployeeRepository>();
            _leaveRepoMock = new Mock<ILeaveRepository>();
            _attendanceRepoMock = new Mock<IAttendanceRepository>();
            _loggerMock = new Mock<ILogger<DepartmentService>>();
            _createValidatorMock = new Mock<IValidator<CreateDepartmentDto>>();
            _updateValidatorMock = new Mock<IValidator<UpdateDepartmentDto>>();

            _unitOfWorkMock.Setup(u => u.Departments).Returns(_departmentRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.Employees).Returns(_employeeRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.Leaves).Returns(_leaveRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.Attendances).Returns(_attendanceRepoMock.Object);

            var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
            _mapper = config.CreateMapper();

            _cache = new MemoryCache(new MemoryCacheOptions());

            _sut = new DepartmentService(
                _unitOfWorkMock.Object,
                _mapper,
                _loggerMock.Object,
                _cache,
                _createValidatorMock.Object,
                _updateValidatorMock.Object);
        }

        [Fact]
        public async Task GetDepartmentByIdAsync_WhenExists_ReturnsDepartmentDto()
        {
            // Arrange
            var department = new Department
            {
                Id = 1,
                Code = "ENG",
                Name = "Engineering",
                Manager = null,
                Employees = new List<Employee>()
            };

            _departmentRepoMock
                .Setup(r => r.GetDepartmentWithManagerAsync(1))
                .ReturnsAsync(department);

            // Act
            var result = await _sut.GetDepartmentByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("Engineering", result.Name);
        }

        [Fact]
        public async Task GetDepartmentByIdAsync_WhenNotFound_ReturnsNull()
        {
            _departmentRepoMock
                .Setup(r => r.GetDepartmentWithManagerAsync(999))
                .ReturnsAsync((Department?)null);

            var result = await _sut.GetDepartmentByIdAsync(999);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetAllDepartmentsAsync_ReturnsAllDepartments()
        {
            // Arrange
            var departments = new List<Department>
            {
                new() { Id = 1, Code = "ENG", Name = "Engineering", Employees = new List<Employee>() },
                new() { Id = 2, Code = "HR",  Name = "Human Resources", Employees = new List<Employee>() }
            };

            _departmentRepoMock
                .Setup(r => r.GetDepartmentsWithEmployeeCountAsync())
                .ReturnsAsync(departments);

            // Act
            var result = await _sut.GetAllDepartmentsAsync();

            // Assert
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task CreateDepartmentAsync_WithValidData_CreatesAndReturnsDepartment()
        {
            // Arrange
            var createDto = new CreateDepartmentDto
            {
                Code = "FIN",
                Name = "Finance"
            };

            _createValidatorMock
                .Setup(v => v.ValidateAsync(createDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _departmentRepoMock
                .Setup(r => r.IsDepartmentCodeUniqueAsync(createDto.Code, It.IsAny<int?>()))
                .ReturnsAsync(true);

            _departmentRepoMock
                .Setup(r => r.IsDepartmentNameUniqueAsync(createDto.Name, It.IsAny<int?>()))
                .ReturnsAsync(true);

            _departmentRepoMock
                .Setup(r => r.AddAsync(It.IsAny<Department>()))
                .Returns(Task.CompletedTask);

            _unitOfWorkMock
                .Setup(u => u.CompleteAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _sut.CreateDepartmentAsync(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("FIN", result.Code);
            Assert.Equal("Finance", result.Name);
            _departmentRepoMock.Verify(r => r.AddAsync(It.IsAny<Department>()), Times.Once);
        }

        [Fact]
        public async Task CreateDepartmentAsync_WithDuplicateCode_ThrowsInvalidOperationException()
        {
            // Arrange
            var createDto = new CreateDepartmentDto { Code = "ENG", Name = "Engineering2" };

            _createValidatorMock
                .Setup(v => v.ValidateAsync(createDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _departmentRepoMock
                .Setup(r => r.IsDepartmentCodeUniqueAsync(createDto.Code, It.IsAny<int?>()))
                .ReturnsAsync(false);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateDepartmentAsync(createDto));
        }

        [Fact]
        public async Task DeleteDepartmentAsync_WhenDepartmentHasEmployees_ReturnsFalse()
        {
            // Arrange
            var department = new Department
            {
                Id = 3,
                Code = "IT",
                Name = "IT",
                Employees = new List<Employee> { new() { Id = 1, FirstName = "Alice", LastName = "A", Email = "a@x.com", JobTitle = "Dev", EmployeeCode = "EMP01" } }
            };

            _departmentRepoMock
                .Setup(r => r.GetDepartmentWithEmployeesAsync(3))
                .ReturnsAsync(department);

            // Act
            var result = await _sut.DeleteDepartmentAsync(3);

            // Assert – service returns false when department has employees
            Assert.False(result);
            _departmentRepoMock.Verify(r => r.Update(It.IsAny<Department>()), Times.Never);
        }
    }
}
