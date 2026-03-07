using HRMS.Core.Entities;
using HRMS.Core.Enums;
using HRMS.Core.Interfaces.Repositories;
using HRMS.Services.Reports;
using Microsoft.Extensions.Logging;
using Moq;

namespace HRMS.UnitTests.Services
{
    public class ReportServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IEmployeeRepository> _employeeRepoMock;
        private readonly Mock<ILeaveRepository> _leaveRepoMock;
        private readonly Mock<IAttendanceRepository> _attendanceRepoMock;
        private readonly Mock<IDepartmentRepository> _departmentRepoMock;
        private readonly Mock<ILogger<ReportService>> _loggerMock;
        private readonly ReportService _sut;

        public ReportServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _employeeRepoMock = new Mock<IEmployeeRepository>();
            _leaveRepoMock = new Mock<ILeaveRepository>();
            _attendanceRepoMock = new Mock<IAttendanceRepository>();
            _departmentRepoMock = new Mock<IDepartmentRepository>();
            _loggerMock = new Mock<ILogger<ReportService>>();

            _unitOfWorkMock.Setup(u => u.Employees).Returns(_employeeRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.Leaves).Returns(_leaveRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.Attendances).Returns(_attendanceRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.Departments).Returns(_departmentRepoMock.Object);

            _sut = new ReportService(_unitOfWorkMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task GetEmployeeReportAsync_ReturnsCorrectCounts()
        {
            // Arrange
            var dept = new Department { Id = 1, Code = "ENG", Name = "Engineering" };
            var employees = new List<Employee>
            {
                BuildEmployee(1, EmployeeStatus.Active, dept, EmploymentType.Permanent, Gender.Male),
                BuildEmployee(2, EmployeeStatus.Active, dept, EmploymentType.Permanent, Gender.Female),
                BuildEmployee(3, EmployeeStatus.Inactive, dept, EmploymentType.Contract, Gender.Male),
                BuildEmployee(4, EmployeeStatus.Terminated, dept, EmploymentType.Contract, Gender.Female),
                BuildEmployee(5, EmployeeStatus.Active, dept, EmploymentType.Intern, Gender.Other,
                    hireDate: DateTime.UtcNow.AddDays(-10)) // recent hire
            };

            _employeeRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(employees);

            // Act
            var result = await _sut.GetEmployeeReportAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.TotalEmployees);
            Assert.Equal(3, result.ActiveEmployees);
            Assert.Equal(1, result.InactiveEmployees);
            Assert.Equal(1, result.TerminatedEmployees);
            Assert.Single(result.NewHires);
            Assert.True(result.EmployeesByDepartment.ContainsKey("Engineering"));
            Assert.Equal(5, result.EmployeesByDepartment["Engineering"]);
        }

        [Fact]
        public async Task GetLeaveReportAsync_ReturnsCorrectSummary()
        {
            // Arrange
            var startDate = new DateTime(2025, 1, 1);
            var endDate = new DateTime(2025, 1, 31);
            var dept = new Department { Id = 1, Code = "ENG", Name = "Engineering" };
            var employee = BuildEmployee(1, EmployeeStatus.Active, dept);

            var leaves = new List<LeaveRequest>
            {
                new() { Id = 1, EmployeeId = 1, Employee = employee, LeaveType = LeaveType.Annual,
                    StartDate = startDate.AddDays(5), EndDate = startDate.AddDays(7),
                    TotalDays = 3, Status = LeaveStatus.Approved, CreatedAt = startDate },
                new() { Id = 2, EmployeeId = 1, Employee = employee, LeaveType = LeaveType.Sick,
                    StartDate = startDate.AddDays(10), EndDate = startDate.AddDays(11),
                    TotalDays = 2, Status = LeaveStatus.Approved, CreatedAt = startDate },
                new() { Id = 3, EmployeeId = 1, Employee = employee, LeaveType = LeaveType.Annual,
                    StartDate = startDate.AddDays(20), EndDate = startDate.AddDays(21),
                    TotalDays = 2, Status = LeaveStatus.Pending, CreatedAt = startDate },
                new() { Id = 4, EmployeeId = 1, Employee = employee, LeaveType = LeaveType.Annual,
                    StartDate = startDate.AddDays(25), EndDate = startDate.AddDays(25),
                    TotalDays = 1, Status = LeaveStatus.Rejected, CreatedAt = startDate },
            };

            _leaveRepoMock.Setup(r => r.GetLeaveRequestsByDateRangeAsync(startDate, endDate))
                .ReturnsAsync(leaves);

            // Act
            var result = await _sut.GetLeaveReportAsync(startDate, endDate);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(4, result.TotalRequests);
            Assert.Equal(2, result.ApprovedRequests);
            Assert.Equal(1, result.PendingRequests);
            Assert.Equal(1, result.RejectedRequests);
            Assert.Equal(5m, result.TotalLeaveDays); // 3 + 2 approved days
            Assert.True(result.LeaveSummary.ContainsKey(LeaveType.Annual));
            Assert.True(result.LeaveSummary.ContainsKey(LeaveType.Sick));
        }

        [Fact]
        public async Task GetAttendanceReportAsync_ReturnsCorrectWorkingDaysCount()
        {
            // Arrange - June 2025 has 21 working days
            var year = 2025;
            var month = 6;
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var employees = new List<Employee>(); // no active employees
            _employeeRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(employees);

            // Act
            var result = await _sut.GetAttendanceReportAsync(year, month);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(new DateTime(year, month, 1), result.Month);
            Assert.True(result.TotalWorkingDays > 0);
            Assert.True(result.TotalWorkingDays <= 23); // June 2025 has 21 weekdays
        }

        [Fact]
        public async Task GetAttendanceReportAsync_IncludesEmployeeAttendanceSummary()
        {
            // Arrange
            var year = 2025;
            var month = 6;
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var dept = new Department { Id = 1, Code = "ENG", Name = "Engineering" };
            var emp = BuildEmployee(1, EmployeeStatus.Active, dept);
            var employees = new List<Employee> { emp };

            var attendanceRecords = new List<Attendance>
            {
                new() { Id = 1, EmployeeId = 1, Date = startDate, Status = AttendanceStatus.Present },
                new() { Id = 2, EmployeeId = 1, Date = startDate.AddDays(1), Status = AttendanceStatus.Late },
                new() { Id = 3, EmployeeId = 1, Date = startDate.AddDays(2), Status = AttendanceStatus.Absent },
            };

            _employeeRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(employees);
            _attendanceRepoMock
                .Setup(r => r.GetAttendanceByEmployeeAsync(1, startDate, endDate))
                .ReturnsAsync(attendanceRecords);
            _attendanceRepoMock
                .Setup(r => r.GetAttendancePercentageAsync(1, startDate, endDate))
                .ReturnsAsync(66.67);

            // Act
            var result = await _sut.GetAttendanceReportAsync(year, month);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.EmployeeAttendance);
            var summary = result.EmployeeAttendance.Values.First();
            // Present field counts Present + Late + HalfDay (all "worked" days)
            Assert.Equal(2, summary.Present);
            Assert.Equal(1, summary.Late);
            Assert.Equal(1, summary.Absent);
        }

        [Fact]
        public async Task ExportEmployeesToExcelAsync_ReturnsCsvBytes()
        {
            // Arrange
            var dept = new Department { Id = 1, Code = "ENG", Name = "Engineering" };
            var employees = new List<Employee>
            {
                BuildEmployee(1, EmployeeStatus.Active, dept),
                BuildEmployee(2, EmployeeStatus.Active, dept)
            };
            _employeeRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(employees);

            // Act
            var result = await _sut.ExportEmployeesToExcelAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Length > 0);

            var content = System.Text.Encoding.UTF8.GetString(result);
            Assert.Contains("Employee Code", content);
            Assert.Contains("EMP00001", content);
            Assert.Contains("EMP00002", content);
        }

        [Fact]
        public async Task ExportLeaveReportToExcelAsync_ReturnsCsvBytes()
        {
            // Arrange
            var startDate = new DateTime(2025, 1, 1);
            var endDate = new DateTime(2025, 1, 31);
            var dept = new Department { Id = 1, Code = "ENG", Name = "Engineering" };
            var emp = BuildEmployee(1, EmployeeStatus.Active, dept);

            var leaves = new List<LeaveRequest>
            {
                new() { Id = 1, EmployeeId = 1, Employee = emp, LeaveType = LeaveType.Annual,
                    StartDate = startDate.AddDays(5), EndDate = startDate.AddDays(7),
                    TotalDays = 3, Status = LeaveStatus.Approved, CreatedAt = startDate }
            };

            _leaveRepoMock.Setup(r => r.GetLeaveRequestsByDateRangeAsync(startDate, endDate))
                .ReturnsAsync(leaves);

            // Act
            var result = await _sut.ExportLeaveReportToExcelAsync(startDate, endDate);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Length > 0);
            var content = System.Text.Encoding.UTF8.GetString(result);
            Assert.Contains("Employee", content);
            Assert.Contains("Annual", content);
        }

        [Fact]
        public async Task ExportAttendanceReportToExcelAsync_ReturnsCsvBytes()
        {
            // Arrange
            var year = 2025;
            var month = 6;
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            _employeeRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Employee>());

            // Act
            var result = await _sut.ExportAttendanceReportToExcelAsync(year, month);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Length > 0);
            var content = System.Text.Encoding.UTF8.GetString(result);
            Assert.Contains("Attendance Report", content);
            Assert.Contains("June 2025", content);
        }

        // ── Helpers ──────────────────────────────────────────────────────────────────

        private static Employee BuildEmployee(
            int id,
            EmployeeStatus status = EmployeeStatus.Active,
            Department? dept = null,
            EmploymentType type = EmploymentType.Permanent,
            Gender gender = Gender.Male,
            DateTime? hireDate = null) => new()
        {
            Id = id,
            EmployeeCode = $"EMP{id:D5}",
            FirstName = "Test",
            LastName = $"User{id}",
            Email = $"test{id}@example.com",
            JobTitle = "Developer",
            DepartmentId = dept?.Id ?? 1,
            Department = dept ?? new Department { Id = 1, Code = "IT", Name = "IT" },
            Status = status,
            EmploymentType = type,
            Gender = gender,
            HireDate = hireDate ?? DateTime.UtcNow.AddYears(-2),
            Salary = 480_000m
        };
    }
}
