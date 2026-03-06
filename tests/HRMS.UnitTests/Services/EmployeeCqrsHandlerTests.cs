using AutoMapper;
using FluentValidation;
using FluentValidation.Results;
using HRMS.Core.Entities;
using HRMS.Core.Enums;
using HRMS.Core.Interfaces.Repositories;
using HRMS.Services.Employees.Commands;
using HRMS.Services.Employees.Dtos;
using HRMS.Services.Employees.Handlers;
using HRMS.Services.Employees.Queries;
using HRMS.Services.Mappings;
using Microsoft.Extensions.Logging;
using Moq;

namespace HRMS.UnitTests.Services
{
    public class EmployeeCqrsHandlerTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IEmployeeRepository> _employeeRepoMock;
        private readonly Mock<IAttendanceRepository> _attendanceRepoMock;
        private readonly IMapper _mapper;

        public EmployeeCqrsHandlerTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _employeeRepoMock = new Mock<IEmployeeRepository>();
            _attendanceRepoMock = new Mock<IAttendanceRepository>();
            _unitOfWorkMock.Setup(u => u.Employees).Returns(_employeeRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.Attendances).Returns(_attendanceRepoMock.Object);

            var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
            _mapper = config.CreateMapper();
        }

        // ------------------------------------------------------------------ GetEmployeeByIdQueryHandler

        [Fact]
        public async Task GetEmployeeByIdQueryHandler_WhenEmployeeExists_ReturnsSuccess()
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
                Department = department
            };

            _employeeRepoMock
                .Setup(r => r.GetEmployeeWithDetailsAsync(1))
                .ReturnsAsync(employee);

            var handler = new GetEmployeeByIdQueryHandler(
                _unitOfWorkMock.Object,
                _mapper,
                new Mock<ILogger<GetEmployeeByIdQueryHandler>>().Object);

            // Act
            var result = await handler.HandleAsync(new GetEmployeeByIdQuery(1));

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal("Alice", result.Value!.FirstName);
        }

        [Fact]
        public async Task GetEmployeeByIdQueryHandler_WhenNotFound_ReturnsSuccessWithNull()
        {
            _employeeRepoMock
                .Setup(r => r.GetEmployeeWithDetailsAsync(999))
                .ReturnsAsync((Employee?)null);

            var handler = new GetEmployeeByIdQueryHandler(
                _unitOfWorkMock.Object,
                _mapper,
                new Mock<ILogger<GetEmployeeByIdQueryHandler>>().Object);

            var result = await handler.HandleAsync(new GetEmployeeByIdQuery(999));

            // The handler returns Success with a null value for "not found"
            Assert.True(result.IsSuccess);
            Assert.Null(result.Value);
        }

        // ------------------------------------------------------------------ CreateEmployeeCommandHandler

        [Fact]
        public async Task CreateEmployeeCommandHandler_WithValidCommand_ReturnsSuccess()
        {
            // Arrange
            var command = new CreateEmployeeCommand
            {
                FirstName = "Bob",
                LastName = "Jones",
                Email = "bob@example.com",
                JobTitle = "QA Engineer",
                DepartmentId = 2,
                DateOfBirth = new DateTime(1990, 6, 15),
                HireDate = DateTime.Today,
                Salary = 4500m
            };

            var validatorMock = new Mock<IValidator<CreateEmployeeDto>>();
            validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<CreateEmployeeDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _employeeRepoMock
                .Setup(r => r.IsEmailUniqueAsync(command.Email, It.IsAny<int?>()))
                .ReturnsAsync(true);

            _employeeRepoMock
                .Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Employee, bool>>>()))
                .ReturnsAsync(5);

            _employeeRepoMock.Setup(r => r.AddAsync(It.IsAny<Employee>())).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            var handler = new CreateEmployeeCommandHandler(
                _unitOfWorkMock.Object,
                _mapper,
                new Mock<ILogger<CreateEmployeeCommandHandler>>().Object,
                validatorMock.Object);

            // Act
            var result = await handler.HandleAsync(command);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("Bob", result.Value.FirstName);
            _employeeRepoMock.Verify(r => r.AddAsync(It.IsAny<Employee>()), Times.Once);
        }

        [Fact]
        public async Task CreateEmployeeCommandHandler_WithDuplicateEmail_ReturnsFailure()
        {
            // Arrange
            var command = new CreateEmployeeCommand
            {
                FirstName = "Carol",
                LastName = "White",
                Email = "existing@example.com",
                JobTitle = "Analyst",
                DepartmentId = 1,
                DateOfBirth = new DateTime(1988, 3, 20),
                HireDate = DateTime.Today
            };

            var validatorMock = new Mock<IValidator<CreateEmployeeDto>>();
            validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<CreateEmployeeDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _employeeRepoMock
                .Setup(r => r.IsEmailUniqueAsync(command.Email, It.IsAny<int?>()))
                .ReturnsAsync(false); // Duplicate!

            var handler = new CreateEmployeeCommandHandler(
                _unitOfWorkMock.Object,
                _mapper,
                new Mock<ILogger<CreateEmployeeCommandHandler>>().Object,
                validatorMock.Object);

            // Act
            var result = await handler.HandleAsync(command);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("already in use", result.Error);
        }

        [Fact]
        public async Task CreateEmployeeCommandHandler_WithValidationErrors_ReturnsFailure()
        {
            // Arrange
            var command = new CreateEmployeeCommand { Email = "not-an-email" };

            var failures = new List<ValidationFailure>
            {
                new("FirstName", "First name is required"),
                new("Email", "Invalid email format")
            };

            var validatorMock = new Mock<IValidator<CreateEmployeeDto>>();
            validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<CreateEmployeeDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(failures));

            var handler = new CreateEmployeeCommandHandler(
                _unitOfWorkMock.Object,
                _mapper,
                new Mock<ILogger<CreateEmployeeCommandHandler>>().Object,
                validatorMock.Object);

            // Act
            var result = await handler.HandleAsync(command);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.NotEmpty(result.Error!);
        }

        // ------------------------------------------------------------------ DeleteEmployeeCommandHandler

        [Fact]
        public async Task DeleteEmployeeCommandHandler_WhenEmployeeExists_ReturnsSuccess()
        {
            // Arrange
            var employee = new Employee
            {
                Id = 5, FirstName = "Dave", LastName = "Green",
                Email = "dave@example.com", JobTitle = "Dev", EmployeeCode = "EMP00005"
            };

            _employeeRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(employee);
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            var handler = new DeleteEmployeeCommandHandler(
                _unitOfWorkMock.Object,
                new Mock<ILogger<DeleteEmployeeCommandHandler>>().Object);

            // Act
            var result = await handler.HandleAsync(new DeleteEmployeeCommand(5));

            // Assert
            Assert.True(result.IsSuccess);
            _employeeRepoMock.Verify(r => r.Remove(employee), Times.Once);
        }

        [Fact]
        public async Task DeleteEmployeeCommandHandler_WhenNotFound_ReturnsFailure()
        {
            _employeeRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Employee?)null);

            var handler = new DeleteEmployeeCommandHandler(
                _unitOfWorkMock.Object,
                new Mock<ILogger<DeleteEmployeeCommandHandler>>().Object);

            var result = await handler.HandleAsync(new DeleteEmployeeCommand(999));

            Assert.False(result.IsSuccess);
            Assert.Contains("Employee", result.Error);
        }
    }
}
