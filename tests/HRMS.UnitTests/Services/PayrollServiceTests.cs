using AutoMapper;
using HRMS.Core.Entities;
using HRMS.Core.Enums;
using HRMS.Core.Interfaces.Repositories;
using HRMS.Core.Interfaces.Services;
using HRMS.Services.Mappings;
using HRMS.Services.Payroll;
using HRMS.Services.Payroll.Dtos;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using PayrollEntity = HRMS.Core.Entities.Payroll;

namespace HRMS.UnitTests.Services
{
    public class PayrollServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IPayrollRepository> _payrollRepoMock;
        private readonly Mock<IEmployeeRepository> _employeeRepoMock;
        private readonly IMapper _mapper;
        private readonly Mock<ILogger<PayrollService>> _loggerMock;
        private readonly IMemoryCache _cache;
        private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;
        private readonly PayrollService _sut;

        private static readonly DateTime FixedNow = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        public PayrollServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _payrollRepoMock = new Mock<IPayrollRepository>();
            _employeeRepoMock = new Mock<IEmployeeRepository>();
            _loggerMock = new Mock<ILogger<PayrollService>>();
            _dateTimeProviderMock = new Mock<IDateTimeProvider>();

            _unitOfWorkMock.Setup(u => u.Payrolls).Returns(_payrollRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.Employees).Returns(_employeeRepoMock.Object);
            _dateTimeProviderMock.Setup(d => d.UtcNow).Returns(FixedNow);

            var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
            _mapper = config.CreateMapper();
            _cache = new MemoryCache(new MemoryCacheOptions());

            _sut = new PayrollService(
                _unitOfWorkMock.Object,
                _mapper,
                _loggerMock.Object,
                _cache,
                _dateTimeProviderMock.Object);
        }

        // ── Income Tax Calculation Tests ─────────────────────────────────────────────

        [Theory]
        [InlineData(0, 0)]
        [InlineData(30_000, 3_000)]       // 10% of 30,000
        [InlineData(50_000, 5_000)]       // 10% of 50,000
        [InlineData(75_000, 10_000)]      // 5,000 + 20% of 25,000 = 10,000
        [InlineData(100_000, 15_000)]     // 5,000 + 20% of 50,000 = 15,000
        [InlineData(150_000, 30_000)]     // 5,000 + 10,000 + 30% of 50,000 = 30,000
        [InlineData(200_000, 45_000)]     // 5,000 + 10,000 + 30,000 = 45,000
        [InlineData(250_000, 62_500)]     // 45,000 + 35% of 50,000 = 62,500
        public void CalculateIncomeTax_ReturnsCorrectTax(decimal income, decimal expectedTax)
        {
            var result = _sut.CalculateIncomeTax(income);
            Assert.Equal(expectedTax, result);
        }

        [Fact]
        public void CalculateIncomeTax_NegativeIncome_ReturnsZero()
        {
            var result = _sut.CalculateIncomeTax(-5_000);
            Assert.Equal(0m, result);
        }

        // ── GetSalaryBreakdownAsync Tests ─────────────────────────────────────────────

        [Fact]
        public async Task GetSalaryBreakdownAsync_ForActiveEmployee_ReturnsValidBreakdown()
        {
            // Arrange
            var employee = BuildEmployee(1, annualSalary: 600_000m); // 50K/month
            _employeeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(employee);

            // Act
            var result = await _sut.GetSalaryBreakdownAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(600_000m, result.AnnualSalary);
            Assert.Equal(50_000m, result.MonthlySalary);
            Assert.True(result.BasicSalary > 0);
            Assert.True(result.GrossSalary > 0);
            Assert.True(result.NetSalary < result.GrossSalary);
            Assert.Equal(result.TotalDeductions,
                result.GrossSalary - result.NetSalary);
        }

        [Fact]
        public async Task GetSalaryBreakdownAsync_WhenEmployeeNotFound_ThrowsKeyNotFoundException()
        {
            _employeeRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Employee?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.GetSalaryBreakdownAsync(999));
        }

        // ── ProcessPayrollAsync Tests ─────────────────────────────────────────────────

        [Fact]
        public async Task ProcessPayrollAsync_WithValidData_CreatesPayrollRecord()
        {
            // Arrange
            var employee = BuildEmployee(1, annualSalary: 600_000m);
            var processDto = new ProcessPayrollDto
            {
                EmployeeId = 1,
                Year = 2025,
                Month = 6,
                WorkingDays = 26,
                PaidDays = 26,
                LopDays = 0
            };

            _employeeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(employee);
            _payrollRepoMock.Setup(r => r.PayrollExistsAsync(1, 2025, 6)).ReturnsAsync(false);
            _payrollRepoMock.Setup(r => r.AddAsync(It.IsAny<PayrollEntity>())).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            var createdPayroll = BuildPayroll(10, employee, PayrollStatus.Processed);
            _payrollRepoMock.Setup(r => r.GetPayrollWithDetailsAsync(It.IsAny<int>())).ReturnsAsync(createdPayroll);

            // Act
            var result = await _sut.ProcessPayrollAsync(processDto, processedById: 99);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(PayrollStatus.Processed, result.Status);
            _payrollRepoMock.Verify(r => r.AddAsync(It.IsAny<PayrollEntity>()), Times.Once);
        }

        [Fact]
        public async Task ProcessPayrollAsync_WhenPayrollAlreadyExists_ThrowsInvalidOperationException()
        {
            // Arrange
            var employee = BuildEmployee(1);
            var processDto = new ProcessPayrollDto { EmployeeId = 1, Year = 2025, Month = 6, WorkingDays = 26, PaidDays = 26 };

            _employeeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(employee);
            _payrollRepoMock.Setup(r => r.PayrollExistsAsync(1, 2025, 6)).ReturnsAsync(true);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ProcessPayrollAsync(processDto, 99));
        }

        [Fact]
        public async Task ProcessPayrollAsync_ForTerminatedEmployee_ThrowsInvalidOperationException()
        {
            // Arrange
            var employee = BuildEmployee(1);
            employee.Status = EmployeeStatus.Terminated;
            var processDto = new ProcessPayrollDto { EmployeeId = 1, Year = 2025, Month = 6, WorkingDays = 26, PaidDays = 26 };

            _employeeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(employee);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ProcessPayrollAsync(processDto, 99));
        }

        // ── ApprovePayrollAsync Tests ──────────────────────────────────────────────────

        [Fact]
        public async Task ApprovePayrollAsync_WhenProcessed_ApprovesSuccessfully()
        {
            // Arrange
            var employee = BuildEmployee(1);
            var payroll = BuildPayroll(5, employee, PayrollStatus.Processed);
            var approvedPayroll = BuildPayroll(5, employee, PayrollStatus.Approved);

            _payrollRepoMock.Setup(r => r.GetPayrollWithDetailsAsync(5)).ReturnsAsync(payroll);
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(1);
            // Simulate the 2nd call returning approved state
            _payrollRepoMock
                .SetupSequence(r => r.GetPayrollWithDetailsAsync(5))
                .ReturnsAsync(payroll)
                .ReturnsAsync(approvedPayroll);

            var approveDto = new ApprovePayrollDto { PayrollId = 5, ApprovedById = 99 };

            // Act
            var result = await _sut.ApprovePayrollAsync(approveDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(PayrollStatus.Approved, result.Status);
        }

        [Fact]
        public async Task ApprovePayrollAsync_WhenNotProcessed_ThrowsInvalidOperationException()
        {
            // Arrange
            var employee = BuildEmployee(1);
            var payroll = BuildPayroll(6, employee, PayrollStatus.Draft);

            _payrollRepoMock.Setup(r => r.GetPayrollWithDetailsAsync(6)).ReturnsAsync(payroll);

            var approveDto = new ApprovePayrollDto { PayrollId = 6, ApprovedById = 99 };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ApprovePayrollAsync(approveDto));
        }

        // ── MarkAsPaidAsync Tests ──────────────────────────────────────────────────────

        [Fact]
        public async Task MarkAsPaidAsync_WhenApproved_MarksAsPaid()
        {
            // Arrange
            var employee = BuildEmployee(1);
            var payroll = BuildPayroll(7, employee, PayrollStatus.Approved);
            var paidPayroll = BuildPayroll(7, employee, PayrollStatus.Paid);
            paidPayroll.PaymentDate = FixedNow;

            _payrollRepoMock.SetupSequence(r => r.GetPayrollWithDetailsAsync(7))
                .ReturnsAsync(payroll)
                .ReturnsAsync(paidPayroll);
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            var dto = new MarkAsPaidDto { PayrollId = 7, PaymentDate = FixedNow, PaymentReference = "TXN-001" };

            // Act
            var result = await _sut.MarkAsPaidAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(PayrollStatus.Paid, result.Status);
        }

        // ── CancelPayrollAsync Tests ──────────────────────────────────────────────────

        [Fact]
        public async Task CancelPayrollAsync_WhenNotPaid_CancelsSuccessfully()
        {
            // Arrange
            var employee = BuildEmployee(1);
            var payroll = BuildPayroll(8, employee, PayrollStatus.Processed);

            _payrollRepoMock.Setup(r => r.GetByIdAsync(8)).ReturnsAsync(payroll);
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            // Act
            var result = await _sut.CancelPayrollAsync(8, "Testing cancellation");

            // Assert
            Assert.True(result);
            Assert.Equal(PayrollStatus.Cancelled, payroll.Status);
        }

        [Fact]
        public async Task CancelPayrollAsync_WhenAlreadyPaid_ThrowsInvalidOperationException()
        {
            // Arrange
            var employee = BuildEmployee(1);
            var payroll = BuildPayroll(9, employee, PayrollStatus.Paid);

            _payrollRepoMock.Setup(r => r.GetByIdAsync(9)).ReturnsAsync(payroll);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CancelPayrollAsync(9, null));
        }

        // ── GetPayrollSummaryAsync Tests ──────────────────────────────────────────────

        [Fact]
        public async Task GetPayrollSummaryAsync_ReturnsCorrectTotals()
        {
            // Arrange
            var employee = BuildEmployee(1);
            var payrolls = new List<PayrollEntity>
            {
                BuildPayroll(1, employee, PayrollStatus.Paid, grossSalary: 50_000m, netSalary: 42_000m),
                BuildPayroll(2, employee, PayrollStatus.Approved, grossSalary: 45_000m, netSalary: 38_000m),
                BuildPayroll(3, employee, PayrollStatus.Processed, grossSalary: 40_000m, netSalary: 34_000m),
                BuildPayroll(4, employee, PayrollStatus.Cancelled, grossSalary: 10_000m, netSalary: 9_000m)
            };

            _payrollRepoMock.Setup(r => r.GetPayrollsByPeriodAsync(2025, 6)).ReturnsAsync(payrolls);

            // Act
            var result = await _sut.GetPayrollSummaryAsync(2025, 6);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(4, result.TotalEmployees);
            Assert.Equal(135_000m, result.TotalGross);   // excludes cancelled
            Assert.Equal(114_000m, result.TotalNet);      // excludes cancelled
            Assert.Equal(1, result.PaidCount);
            Assert.Equal(1, result.ApprovedCount);
            Assert.Equal(1, result.ProcessedCount);
        }

        // ── LOp deduction calculation test ────────────────────────────────────────────

        [Fact]
        public async Task ProcessPayrollAsync_WithLopDays_DeductsCorrectly()
        {
            // Arrange - employee earns 600K/yr; 2 LOP days out of 26 working days
            var employee = BuildEmployee(1, annualSalary: 600_000m);
            var processDto = new ProcessPayrollDto
            {
                EmployeeId = 1,
                Year = 2025,
                Month = 6,
                WorkingDays = 26,
                PaidDays = 24,
                LopDays = 2
            };

            _employeeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(employee);
            _payrollRepoMock.Setup(r => r.PayrollExistsAsync(1, 2025, 6)).ReturnsAsync(false);

            PayrollEntity? capturedPayroll = null;
            _payrollRepoMock.Setup(r => r.AddAsync(It.IsAny<PayrollEntity>()))
                .Callback<PayrollEntity>(p => capturedPayroll = p)
                .Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(1);
            _payrollRepoMock.Setup(r => r.GetPayrollWithDetailsAsync(It.IsAny<int>()))
                .ReturnsAsync(() => capturedPayroll ?? BuildPayroll(99, employee));

            // Act
            await _sut.ProcessPayrollAsync(processDto, 99);

            // Assert
            Assert.NotNull(capturedPayroll);
            Assert.Equal(2, capturedPayroll!.LopDays);
            Assert.True(capturedPayroll.LopDeduction > 0, "LOP deduction should be positive");
        }

        // ── Helpers ──────────────────────────────────────────────────────────────────

        private static Employee BuildEmployee(int id, decimal annualSalary = 480_000m) => new()
        {
            Id = id,
            EmployeeCode = $"EMP{id:D5}",
            FirstName = "Test",
            LastName = "User",
            Email = $"test{id}@example.com",
            JobTitle = "Developer",
            DepartmentId = 1,
            Department = new Department { Id = 1, Code = "ENG", Name = "Engineering" },
            Salary = annualSalary,
            Status = EmployeeStatus.Active
        };

        private static PayrollEntity BuildPayroll(
            int id,
            Employee employee,
            PayrollStatus status = PayrollStatus.Draft,
            decimal grossSalary = 40_000m,
            decimal netSalary = 34_000m) => new()
        {
            Id = id,
            EmployeeId = employee.Id,
            Employee = employee,
            Year = 2025,
            Month = 6,
            Status = status,
            BasicSalary = 20_000m,
            HouseRentAllowance = 8_000m,
            ConveyanceAllowance = 1_600m,
            MedicalAllowance = 1_250m,
            GrossSalary = grossSalary,
            NetSalary = netSalary,
            TotalDeductions = grossSalary - netSalary,
            WorkingDays = 26,
            PaidDays = 26,
            CreatedAt = FixedNow
        };
    }
}
