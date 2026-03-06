using AutoMapper;
using FluentValidation;
using FluentValidation.Results;
using HRMS.Core.Entities;
using HRMS.Core.Enums;
using HRMS.Core.Interfaces.Repositories;
using HRMS.Services.Leave;
using HRMS.Services.Leave.Dtos;
using HRMS.Services.Mappings;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace HRMS.UnitTests.Services
{
    public class LeaveServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ILeaveRepository> _leaveRepoMock;
        private readonly Mock<IEmployeeRepository> _employeeRepoMock;
        private readonly Mock<IDepartmentRepository> _departmentRepoMock;
        private readonly Mock<IAttendanceRepository> _attendanceRepoMock;
        private readonly IMapper _mapper;
        private readonly Mock<ILogger<LeaveService>> _loggerMock;
        private readonly IMemoryCache _cache;
        private readonly Mock<IValidator<CreateLeaveRequestDto>> _createValidatorMock;
        private readonly Mock<IValidator<ApproveLeaveDto>> _approveValidatorMock;
        private readonly Mock<IValidator<RejectLeaveDto>> _rejectValidatorMock;
        private readonly LeaveService _sut;

        public LeaveServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _leaveRepoMock = new Mock<ILeaveRepository>();
            _employeeRepoMock = new Mock<IEmployeeRepository>();
            _departmentRepoMock = new Mock<IDepartmentRepository>();
            _attendanceRepoMock = new Mock<IAttendanceRepository>();
            _loggerMock = new Mock<ILogger<LeaveService>>();
            _createValidatorMock = new Mock<IValidator<CreateLeaveRequestDto>>();
            _approveValidatorMock = new Mock<IValidator<ApproveLeaveDto>>();
            _rejectValidatorMock = new Mock<IValidator<RejectLeaveDto>>();

            _unitOfWorkMock.Setup(u => u.Leaves).Returns(_leaveRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.Employees).Returns(_employeeRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.Departments).Returns(_departmentRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.Attendances).Returns(_attendanceRepoMock.Object);

            var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
            _mapper = config.CreateMapper();

            _cache = new MemoryCache(new MemoryCacheOptions());

            _sut = new LeaveService(
                _unitOfWorkMock.Object,
                _mapper,
                _loggerMock.Object,
                _cache,
                _createValidatorMock.Object,
                _approveValidatorMock.Object,
                _rejectValidatorMock.Object);
        }

        [Fact]
        public async Task GetLeaveRequestByIdAsync_WhenExists_ReturnsDto()
        {
            // Arrange
            var department = new Department { Id = 1, Code = "ENG", Name = "Engineering" };
            var employee = new Employee
            {
                Id = 1,
                EmployeeCode = "EMP00001",
                FirstName = "Alice",
                LastName = "Smith",
                Email = "alice@example.com",
                JobTitle = "Dev",
                DepartmentId = 1,
                Department = department
            };
            var leaveRequest = new LeaveRequest
            {
                Id = 1,
                EmployeeId = 1,
                Employee = employee,
                LeaveType = LeaveType.Annual,
                StartDate = DateTime.Today.AddDays(7),
                EndDate = DateTime.Today.AddDays(9),
                TotalDays = 3,
                Status = LeaveStatus.Pending
            };

            _leaveRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(leaveRequest);

            // Act
            var result = await _sut.GetLeaveRequestByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal(LeaveType.Annual, result.LeaveType);
            Assert.Equal(LeaveStatus.Pending, result.Status);
        }

        [Fact]
        public async Task GetLeaveRequestByIdAsync_WhenNotFound_ReturnsNull()
        {
            _leaveRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((LeaveRequest?)null);

            var result = await _sut.GetLeaveRequestByIdAsync(999);

            Assert.Null(result);
        }

        [Fact]
        public async Task CreateLeaveRequestAsync_WithValidData_CreatesLeaveRequest()
        {
            // Arrange
            var createDto = new CreateLeaveRequestDto
            {
                EmployeeId = 1,
                LeaveType = LeaveType.Annual,
                StartDate = DateTime.Today.AddDays(14),
                EndDate = DateTime.Today.AddDays(16),
                IsPaid = true
            };

            var employee = new Employee
            {
                Id = 1, EmployeeCode = "EMP00001", FirstName = "Alice", LastName = "Smith",
                Email = "alice@example.com", JobTitle = "Dev", DepartmentId = 1
            };

            _createValidatorMock
                .Setup(v => v.ValidateAsync(createDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _employeeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(employee);

            _leaveRepoMock
                .Setup(r => r.CanApplyLeaveAsync(1, LeaveType.Annual, createDto.StartDate, createDto.EndDate))
                .ReturnsAsync(true);

            _leaveRepoMock
                .Setup(r => r.HasOverlappingLeaveAsync(1, createDto.StartDate, createDto.EndDate, It.IsAny<int?>()))
                .ReturnsAsync(false);

            _leaveRepoMock.Setup(r => r.AddAsync(It.IsAny<LeaveRequest>())).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            // Act
            var result = await _sut.CreateLeaveRequestAsync(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(LeaveStatus.Pending, result.Status);
            Assert.Equal(LeaveType.Annual, result.LeaveType);
            _leaveRepoMock.Verify(r => r.AddAsync(It.IsAny<LeaveRequest>()), Times.Once);
        }

        [Fact]
        public async Task CreateLeaveRequestAsync_WithOverlappingLeave_ThrowsInvalidOperationException()
        {
            // Arrange
            var createDto = new CreateLeaveRequestDto
            {
                EmployeeId = 2,
                LeaveType = LeaveType.Annual,
                StartDate = DateTime.Today.AddDays(5),
                EndDate = DateTime.Today.AddDays(7)
            };

            var employee = new Employee
            {
                Id = 2, EmployeeCode = "EMP00002", FirstName = "Bob", LastName = "Jones",
                Email = "bob@example.com", JobTitle = "QA", DepartmentId = 1
            };

            _createValidatorMock
                .Setup(v => v.ValidateAsync(createDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _employeeRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(employee);

            _leaveRepoMock
                .Setup(r => r.CanApplyLeaveAsync(2, LeaveType.Annual, createDto.StartDate, createDto.EndDate))
                .ReturnsAsync(true);

            _leaveRepoMock
                .Setup(r => r.HasOverlappingLeaveAsync(2, createDto.StartDate, createDto.EndDate, It.IsAny<int?>()))
                .ReturnsAsync(true); // Overlapping!

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateLeaveRequestAsync(createDto));
        }

        [Fact]
        public async Task ApproveLeaveRequestAsync_WithPendingRequest_ApprovesSuccessfully()
        {
            // Arrange
            var approveDto = new ApproveLeaveDto { Id = 10, Remarks = "Approved" };

            var department = new Department { Id = 1, Code = "IT", Name = "IT" };
            var employee = new Employee
            {
                Id = 3, EmployeeCode = "EMP00003", FirstName = "Carol", LastName = "White",
                Email = "carol@example.com", JobTitle = "Dev", DepartmentId = 1, Department = department
            };
            var approver = new Employee
            {
                Id = 99, EmployeeCode = "EMP00099", FirstName = "Manager", LastName = "Boss",
                Email = "boss@example.com", JobTitle = "Manager", DepartmentId = 1, Department = department
            };
            var leaveRequest = new LeaveRequest
            {
                Id = 10,
                EmployeeId = 3,
                Employee = employee,
                LeaveType = LeaveType.Annual,
                StartDate = DateTime.Today.AddDays(5),
                EndDate = DateTime.Today.AddDays(7),
                TotalDays = 3,
                Status = LeaveStatus.Pending
            };

            _approveValidatorMock
                .Setup(v => v.ValidateAsync(approveDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _leaveRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(leaveRequest);
            _employeeRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync(approver);
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            // Act
            var result = await _sut.ApproveLeaveRequestAsync(approveDto, 99);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(LeaveStatus.Approved, result.Status);
            _unitOfWorkMock.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task ApproveLeaveRequestAsync_WhenAlreadyApproved_ThrowsInvalidOperationException()
        {
            // Arrange
            var approveDto = new ApproveLeaveDto { Id = 20 };

            var department = new Department { Id = 1, Code = "ENG", Name = "Engineering" };
            var employee = new Employee
            {
                Id = 4, EmployeeCode = "EMP00004", FirstName = "Dan", LastName = "Blue",
                Email = "dan@example.com", JobTitle = "Analyst", DepartmentId = 1, Department = department
            };
            var leaveRequest = new LeaveRequest
            {
                Id = 20,
                EmployeeId = 4,
                Employee = employee,
                Status = LeaveStatus.Approved // Already approved
            };

            _approveValidatorMock
                .Setup(v => v.ValidateAsync(approveDto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _leaveRepoMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(leaveRequest);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _sut.ApproveLeaveRequestAsync(approveDto, 99));
        }
    }
}
