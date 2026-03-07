using AutoMapper;
using HRMS.Core.Entities;
using HRMS.Core.Enums;
using HRMS.Core.Interfaces.Repositories;
using HRMS.Core.Interfaces.Services;
using HRMS.Services.Attendance;
using HRMS.Services.Attendance.Dtos;
using HRMS.Services.Mappings;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace HRMS.UnitTests.Services
{
    public class AttendanceServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IAttendanceRepository> _attendanceRepoMock;
        private readonly Mock<IEmployeeRepository> _employeeRepoMock;
        private readonly IMapper _mapper;
        private readonly Mock<ILogger<AttendanceService>> _loggerMock;
        private readonly IMemoryCache _cache;
        private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;
        private readonly AttendanceService _sut;

        private static readonly DateTime FixedNow = new DateTime(2025, 6, 15, 9, 0, 0, DateTimeKind.Utc);

        public AttendanceServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _attendanceRepoMock = new Mock<IAttendanceRepository>();
            _employeeRepoMock = new Mock<IEmployeeRepository>();
            _loggerMock = new Mock<ILogger<AttendanceService>>();
            _dateTimeProviderMock = new Mock<IDateTimeProvider>();

            _unitOfWorkMock.Setup(u => u.Attendances).Returns(_attendanceRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.Employees).Returns(_employeeRepoMock.Object);
            _dateTimeProviderMock.Setup(d => d.UtcNow).Returns(FixedNow);

            var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
            _mapper = config.CreateMapper();
            _cache = new MemoryCache(new MemoryCacheOptions());

            _sut = new AttendanceService(
                _unitOfWorkMock.Object,
                _mapper,
                _loggerMock.Object,
                _cache,
                _dateTimeProviderMock.Object);
        }

        [Fact]
        public async Task GetAttendanceByIdAsync_WhenExists_ReturnsDto()
        {
            // Arrange
            var employee = BuildEmployee(1);
            var attendance = new Attendance
            {
                Id = 1,
                EmployeeId = 1,
                Employee = employee,
                Date = FixedNow.Date,
                Status = AttendanceStatus.Present
            };
            _attendanceRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(attendance);

            // Act
            var result = await _sut.GetAttendanceByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal(AttendanceStatus.Present, result.Status);
        }

        [Fact]
        public async Task GetAttendanceByIdAsync_WhenNotFound_ReturnsNull()
        {
            _attendanceRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Attendance?)null);

            var result = await _sut.GetAttendanceByIdAsync(999);

            Assert.Null(result);
        }

        [Fact]
        public async Task CreateAttendanceAsync_WhenNoExistingRecord_CreatesSuccessfully()
        {
            // Arrange
            var createDto = new CreateAttendanceDto
            {
                EmployeeId = 1,
                Date = FixedNow.Date,
                CheckInTime = FixedNow,
                Status = AttendanceStatus.Present
            };

            _attendanceRepoMock
                .Setup(r => r.GetAttendanceByEmployeeAndDateAsync(1, FixedNow.Date))
                .ReturnsAsync((Attendance?)null);
            _attendanceRepoMock.Setup(r => r.AddAsync(It.IsAny<Attendance>())).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            // Act
            var result = await _sut.CreateAttendanceAsync(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(AttendanceStatus.Present, result.Status);
            _attendanceRepoMock.Verify(r => r.AddAsync(It.IsAny<Attendance>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateAttendanceAsync_WhenRecordAlreadyExists_ThrowsInvalidOperationException()
        {
            // Arrange
            var createDto = new CreateAttendanceDto
            {
                EmployeeId = 1,
                Date = FixedNow.Date,
                Status = AttendanceStatus.Present
            };
            var existing = new Attendance { Id = 5, EmployeeId = 1, Date = FixedNow.Date };

            _attendanceRepoMock
                .Setup(r => r.GetAttendanceByEmployeeAndDateAsync(1, FixedNow.Date))
                .ReturnsAsync(existing);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateAttendanceAsync(createDto));
        }

        [Fact]
        public async Task CheckInAsync_ForNewDay_CreatesAttendanceRecord()
        {
            // Arrange
            var checkInDto = new CheckInDto
            {
                EmployeeId = 1,
                CheckInTime = FixedNow // 09:00 - on time
            };

            _attendanceRepoMock
                .Setup(r => r.GetAttendanceByEmployeeAndDateAsync(1, FixedNow.Date))
                .ReturnsAsync((Attendance?)null);
            _attendanceRepoMock.Setup(r => r.AddAsync(It.IsAny<Attendance>())).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            // Act
            var result = await _sut.CheckInAsync(checkInDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(AttendanceStatus.Present, result.Status);
            Assert.Equal(0m, result.LateMinutes);
        }

        [Fact]
        public async Task CheckInAsync_WhenLate_SetsLateStatus()
        {
            // Arrange
            var lateCheckIn = FixedNow.Date.Add(TimeSpan.FromHours(10)); // 10:00 AM - late
            var checkInDto = new CheckInDto { EmployeeId = 1, CheckInTime = lateCheckIn };

            _attendanceRepoMock
                .Setup(r => r.GetAttendanceByEmployeeAndDateAsync(1, lateCheckIn.Date))
                .ReturnsAsync((Attendance?)null);
            _attendanceRepoMock.Setup(r => r.AddAsync(It.IsAny<Attendance>())).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            // Act
            var result = await _sut.CheckInAsync(checkInDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(AttendanceStatus.Late, result.Status);
            Assert.True(result.LateMinutes > 0);
        }

        [Fact]
        public async Task CheckInAsync_WhenAlreadyCheckedIn_ThrowsInvalidOperationException()
        {
            // Arrange
            var existing = new Attendance
            {
                Id = 1, EmployeeId = 1, Date = FixedNow.Date, CheckInTime = FixedNow.AddHours(-1)
            };
            var checkInDto = new CheckInDto { EmployeeId = 1, CheckInTime = FixedNow };

            _attendanceRepoMock
                .Setup(r => r.GetAttendanceByEmployeeAndDateAsync(1, FixedNow.Date))
                .ReturnsAsync(existing);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CheckInAsync(checkInDto));
        }

        [Fact]
        public async Task CheckOutAsync_WhenCheckedIn_RecordsCheckOut()
        {
            // Arrange
            var checkOut = FixedNow.AddHours(8);
            var existing = new Attendance
            {
                Id = 1, EmployeeId = 1, Date = FixedNow.Date, CheckInTime = FixedNow, Status = AttendanceStatus.Present
            };
            var checkOutDto = new CheckOutDto { EmployeeId = 1, CheckOutTime = checkOut };

            _attendanceRepoMock
                .Setup(r => r.GetAttendanceByEmployeeAndDateAsync(1, checkOut.Date))
                .ReturnsAsync(existing);
            _attendanceRepoMock.Setup(r => r.AddAsync(It.IsAny<Attendance>())).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            // Act
            var result = await _sut.CheckOutAsync(checkOutDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(checkOut, result.CheckOutTime);
        }

        [Fact]
        public async Task CheckOutAsync_WhenNoCheckIn_ThrowsInvalidOperationException()
        {
            // Arrange
            var checkOutDto = new CheckOutDto { EmployeeId = 1, CheckOutTime = FixedNow };

            _attendanceRepoMock
                .Setup(r => r.GetAttendanceByEmployeeAndDateAsync(1, FixedNow.Date))
                .ReturnsAsync((Attendance?)null);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CheckOutAsync(checkOutDto));
        }

        [Fact]
        public async Task GetMonthlySummaryAsync_ReturnsSummaryWithCorrectCounts()
        {
            // Arrange
            var employeeId = 1;
            var year = 2025;
            var month = 6;
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var employee = BuildEmployee(employeeId);
            var records = new List<Attendance>
            {
                new() { Id = 1, EmployeeId = employeeId, Date = startDate, Status = AttendanceStatus.Present },
                new() { Id = 2, EmployeeId = employeeId, Date = startDate.AddDays(1), Status = AttendanceStatus.Late },
                new() { Id = 3, EmployeeId = employeeId, Date = startDate.AddDays(2), Status = AttendanceStatus.Absent },
            };

            _employeeRepoMock.Setup(r => r.GetByIdAsync(employeeId)).ReturnsAsync(employee);
            _attendanceRepoMock
                .Setup(r => r.GetAttendanceByEmployeeAsync(employeeId, startDate, endDate))
                .ReturnsAsync(records);
            _attendanceRepoMock
                .Setup(r => r.GetAttendancePercentageAsync(employeeId, startDate, endDate))
                .ReturnsAsync(66.67);

            // Act
            var result = await _sut.GetMonthlySummaryAsync(employeeId, year, month);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(employeeId, result.EmployeeId);
            Assert.Equal(3, result.TotalDays);
            Assert.Equal(1, result.PresentDays);
            Assert.Equal(1, result.AbsentDays);
            Assert.Equal(1, result.LateDays);
        }

        [Fact]
        public async Task DeleteAttendanceAsync_WhenExists_SoftDeletes()
        {
            // Arrange
            var attendance = new Attendance { Id = 1, EmployeeId = 1, Date = FixedNow.Date };
            _attendanceRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(attendance);
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            // Act
            var result = await _sut.DeleteAttendanceAsync(1);

            // Assert
            Assert.True(result);
            Assert.True(attendance.IsDeleted);
        }

        [Fact]
        public async Task DeleteAttendanceAsync_WhenNotFound_ThrowsKeyNotFoundException()
        {
            _attendanceRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Attendance?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.DeleteAttendanceAsync(999));
        }

        private static Employee BuildEmployee(int id) => new()
        {
            Id = id,
            EmployeeCode = $"EMP{id:D5}",
            FirstName = "Test",
            LastName = "User",
            Email = $"test{id}@example.com",
            JobTitle = "Dev",
            DepartmentId = 1,
            Department = new Department { Id = 1, Code = "ENG", Name = "Engineering" }
        };
    }
}
