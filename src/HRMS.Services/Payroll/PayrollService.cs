using AutoMapper;
using HRMS.Core.Enums;
using HRMS.Core.Interfaces.Repositories;
using HRMS.Core.Interfaces.Services;
using HRMS.Services.Payroll.Dtos;
using HRMS.Shared.Constants;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace HRMS.Services.Payroll
{
    /// <summary>
    /// Service for payroll processing, salary calculation, and tax computation.
    /// </summary>
    public class PayrollService : IPayrollService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<PayrollService> _logger;
        private readonly IMemoryCache _cache;
        private readonly IDateTimeProvider _dateTimeProvider;

        public PayrollService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<PayrollService> logger,
            IMemoryCache cache,
            IDateTimeProvider dateTimeProvider)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _cache = cache;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<PayrollDto?> GetPayrollByIdAsync(int id)
        {
            try
            {
                var cacheKey = HrmsConstants.Payroll.PayrollKey(id);
                if (_cache.TryGetValue(cacheKey, out PayrollDto? cached))
                    return cached;

                var payroll = await _unitOfWork.Payrolls.GetPayrollWithDetailsAsync(id);
                if (payroll == null) return null;

                var dto = _mapper.Map<PayrollDto>(payroll);
                _cache.Set(cacheKey, dto, TimeSpan.FromMinutes(HrmsConstants.Cache.DefaultExpirationMinutes));
                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payroll {PayrollId}", id);
                throw;
            }
        }

        public async Task<PayrollDto?> GetPayrollByEmployeeAndPeriodAsync(int employeeId, int year, int month)
        {
            try
            {
                var cacheKey = HrmsConstants.Payroll.EmployeePayrollKey(employeeId, year, month);
                if (_cache.TryGetValue(cacheKey, out PayrollDto? cached))
                    return cached;

                var payroll = await _unitOfWork.Payrolls.GetPayrollByEmployeeAndPeriodAsync(employeeId, year, month);
                if (payroll == null) return null;

                var dto = _mapper.Map<PayrollDto>(payroll);
                _cache.Set(cacheKey, dto, TimeSpan.FromMinutes(HrmsConstants.Cache.DefaultExpirationMinutes));
                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payroll for employee {EmployeeId} period {Year}/{Month}", employeeId, year, month);
                throw;
            }
        }

        public async Task<IEnumerable<PayrollDto>> GetPayrollsByEmployeeAsync(int employeeId)
        {
            try
            {
                var payrolls = await _unitOfWork.Payrolls.GetPayrollsByEmployeeAsync(employeeId);
                return _mapper.Map<IEnumerable<PayrollDto>>(payrolls);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payrolls for employee {EmployeeId}", employeeId);
                throw;
            }
        }

        public async Task<IEnumerable<PayrollDto>> GetPayrollsByPeriodAsync(int year, int month)
        {
            try
            {
                var payrolls = await _unitOfWork.Payrolls.GetPayrollsByPeriodAsync(year, month);
                return _mapper.Map<IEnumerable<PayrollDto>>(payrolls);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payrolls for period {Year}/{Month}", year, month);
                throw;
            }
        }

        public async Task<PayrollDto> ProcessPayrollAsync(ProcessPayrollDto processDto, int processedById)
        {
            try
            {
                _logger.LogInformation("Processing payroll for employee {EmployeeId} period {Year}/{Month}",
                    processDto.EmployeeId, processDto.Year, processDto.Month);

                var employee = await _unitOfWork.Employees.GetByIdAsync(processDto.EmployeeId);
                if (employee == null)
                    throw new KeyNotFoundException($"Employee with ID {processDto.EmployeeId} not found");

                if (employee.Status == EmployeeStatus.Terminated || employee.Status == EmployeeStatus.Resigned)
                    throw new InvalidOperationException($"Cannot process payroll for {employee.Status} employee");

                var exists = await _unitOfWork.Payrolls.PayrollExistsAsync(
                    processDto.EmployeeId, processDto.Year, processDto.Month);
                if (exists)
                    throw new InvalidOperationException(
                        $"Payroll already exists for employee {processDto.EmployeeId} for {processDto.Year}/{processDto.Month:D2}");

                var payroll = CalculatePayroll(employee.Salary, processDto);
                payroll.EmployeeId = processDto.EmployeeId;
                payroll.Year = processDto.Year;
                payroll.Month = processDto.Month;
                payroll.PayFrequency = PayFrequency.Monthly;
                payroll.Status = PayrollStatus.Processed;
                payroll.ProcessedById = processedById;
                payroll.ProcessedDate = _dateTimeProvider.UtcNow;
                payroll.Remarks = processDto.Remarks;
                payroll.CreatedAt = _dateTimeProvider.UtcNow;

                await _unitOfWork.Payrolls.AddAsync(payroll);
                await _unitOfWork.CompleteAsync();

                _logger.LogInformation("Payroll processed with ID {PayrollId}", payroll.Id);
                InvalidatePayrollCache(payroll.Id, payroll.EmployeeId, payroll.Year, payroll.Month);

                var created = await _unitOfWork.Payrolls.GetPayrollWithDetailsAsync(payroll.Id);
                return _mapper.Map<PayrollDto>(created!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payroll for employee {EmployeeId}", processDto.EmployeeId);
                throw;
            }
        }

        public async Task<int> BulkProcessPayrollAsync(BulkProcessPayrollDto bulkDto)
        {
            try
            {
                _logger.LogInformation("Bulk processing payroll for period {Year}/{Month}", bulkDto.Year, bulkDto.Month);

                var employees = await _unitOfWork.Employees.GetAllAsync();
                var activeEmployees = employees.Where(e =>
                    e.Status == EmployeeStatus.Active || e.Status == EmployeeStatus.OnLeave);

                if (bulkDto.DepartmentId.HasValue)
                    activeEmployees = activeEmployees.Where(e => e.DepartmentId == bulkDto.DepartmentId.Value);

                int processed = 0;
                foreach (var employee in activeEmployees)
                {
                    var exists = await _unitOfWork.Payrolls.PayrollExistsAsync(
                        employee.Id, bulkDto.Year, bulkDto.Month);
                    if (exists) continue;

                    var processDto = new ProcessPayrollDto
                    {
                        EmployeeId = employee.Id,
                        Year = bulkDto.Year,
                        Month = bulkDto.Month,
                        WorkingDays = bulkDto.WorkingDays,
                        PaidDays = bulkDto.WorkingDays,
                        LopDays = 0
                    };

                    var payroll = CalculatePayroll(employee.Salary, processDto);
                    payroll.EmployeeId = employee.Id;
                    payroll.Year = bulkDto.Year;
                    payroll.Month = bulkDto.Month;
                    payroll.PayFrequency = PayFrequency.Monthly;
                    payroll.Status = PayrollStatus.Processed;
                    payroll.ProcessedById = bulkDto.ProcessedById;
                    payroll.ProcessedDate = _dateTimeProvider.UtcNow;
                    payroll.CreatedAt = _dateTimeProvider.UtcNow;

                    await _unitOfWork.Payrolls.AddAsync(payroll);
                    processed++;
                }

                await _unitOfWork.CompleteAsync();
                _logger.LogInformation("Bulk payroll processed: {Count} records", processed);
                return processed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk payroll processing");
                throw;
            }
        }

        public async Task<PayrollDto> ApprovePayrollAsync(ApprovePayrollDto approveDto)
        {
            try
            {
                var payroll = await _unitOfWork.Payrolls.GetPayrollWithDetailsAsync(approveDto.PayrollId);
                if (payroll == null)
                    throw new KeyNotFoundException($"Payroll with ID {approveDto.PayrollId} not found");

                if (payroll.Status != PayrollStatus.Processed)
                    throw new InvalidOperationException($"Only processed payrolls can be approved. Current status: {payroll.Status}");

                payroll.Status = PayrollStatus.Approved;
                payroll.ApprovedById = approveDto.ApprovedById;
                payroll.ApprovedDate = _dateTimeProvider.UtcNow;
                if (!string.IsNullOrEmpty(approveDto.Remarks)) payroll.Remarks = approveDto.Remarks;
                payroll.UpdatedAt = _dateTimeProvider.UtcNow;

                _unitOfWork.Payrolls.Update(payroll);
                await _unitOfWork.CompleteAsync();

                InvalidatePayrollCache(payroll.Id, payroll.EmployeeId, payroll.Year, payroll.Month);

                var updated = await _unitOfWork.Payrolls.GetPayrollWithDetailsAsync(payroll.Id);
                return _mapper.Map<PayrollDto>(updated!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving payroll {PayrollId}", approveDto.PayrollId);
                throw;
            }
        }

        public async Task<PayrollDto> MarkAsPaidAsync(MarkAsPaidDto markAsPaidDto)
        {
            try
            {
                var payroll = await _unitOfWork.Payrolls.GetPayrollWithDetailsAsync(markAsPaidDto.PayrollId);
                if (payroll == null)
                    throw new KeyNotFoundException($"Payroll with ID {markAsPaidDto.PayrollId} not found");

                if (payroll.Status != PayrollStatus.Approved)
                    throw new InvalidOperationException($"Only approved payrolls can be marked as paid. Current status: {payroll.Status}");

                payroll.Status = PayrollStatus.Paid;
                payroll.PaymentDate = markAsPaidDto.PaymentDate;
                payroll.PaymentReference = markAsPaidDto.PaymentReference;
                payroll.UpdatedAt = _dateTimeProvider.UtcNow;

                _unitOfWork.Payrolls.Update(payroll);
                await _unitOfWork.CompleteAsync();

                InvalidatePayrollCache(payroll.Id, payroll.EmployeeId, payroll.Year, payroll.Month);

                var updated = await _unitOfWork.Payrolls.GetPayrollWithDetailsAsync(payroll.Id);
                return _mapper.Map<PayrollDto>(updated!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking payroll {PayrollId} as paid", markAsPaidDto.PayrollId);
                throw;
            }
        }

        public async Task<bool> CancelPayrollAsync(int payrollId, string? reason)
        {
            try
            {
                var payroll = await _unitOfWork.Payrolls.GetByIdAsync(payrollId);
                if (payroll == null)
                    throw new KeyNotFoundException($"Payroll with ID {payrollId} not found");

                if (payroll.Status == PayrollStatus.Paid)
                    throw new InvalidOperationException("Cannot cancel a payroll that has already been paid");

                payroll.Status = PayrollStatus.Cancelled;
                payroll.Remarks = reason;
                payroll.UpdatedAt = _dateTimeProvider.UtcNow;

                _unitOfWork.Payrolls.Update(payroll);
                await _unitOfWork.CompleteAsync();

                InvalidatePayrollCache(payroll.Id, payroll.EmployeeId, payroll.Year, payroll.Month);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling payroll {PayrollId}", payrollId);
                throw;
            }
        }

        public async Task<PayrollSummaryDto> GetPayrollSummaryAsync(int year, int month)
        {
            try
            {
                var payrolls = (await _unitOfWork.Payrolls.GetPayrollsByPeriodAsync(year, month)).ToList();

                return new PayrollSummaryDto
                {
                    Year = year,
                    Month = month,
                    TotalEmployees = payrolls.Count,
                    TotalGross = payrolls.Where(p => p.Status != PayrollStatus.Cancelled).Sum(p => p.GrossSalary),
                    TotalDeductions = payrolls.Where(p => p.Status != PayrollStatus.Cancelled).Sum(p => p.TotalDeductions),
                    TotalNet = payrolls.Where(p => p.Status != PayrollStatus.Cancelled).Sum(p => p.NetSalary),
                    TotalEmployerContributions = payrolls
                        .Where(p => p.Status != PayrollStatus.Cancelled)
                        .Sum(p => p.EmployerProvidentFund + p.EmployerEsi),
                    ProcessedCount = payrolls.Count(p => p.Status == PayrollStatus.Processed),
                    ApprovedCount = payrolls.Count(p => p.Status == PayrollStatus.Approved),
                    PaidCount = payrolls.Count(p => p.Status == PayrollStatus.Paid),
                    PendingCount = payrolls.Count(p => p.Status == PayrollStatus.Draft)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payroll summary for {Year}/{Month}", year, month);
                throw;
            }
        }

        public async Task<SalaryBreakdownDto> GetSalaryBreakdownAsync(int employeeId)
        {
            try
            {
                var employee = await _unitOfWork.Employees.GetByIdAsync(employeeId);
                if (employee == null)
                    throw new KeyNotFoundException($"Employee with ID {employeeId} not found");

                return ComputeSalaryBreakdown(employee.Salary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting salary breakdown for employee {EmployeeId}", employeeId);
                throw;
            }
        }

        /// <summary>
        /// Calculates income tax using a progressive bracket system.
        /// </summary>
        public decimal CalculateIncomeTax(decimal annualTaxableIncome)
        {
            if (annualTaxableIncome <= 0) return 0m;

            decimal tax = 0m;

            if (annualTaxableIncome <= HrmsConstants.Payroll.TaxBracket1Limit)
            {
                tax = annualTaxableIncome * HrmsConstants.Payroll.TaxRate1;
            }
            else if (annualTaxableIncome <= HrmsConstants.Payroll.TaxBracket2Limit)
            {
                tax = HrmsConstants.Payroll.TaxBracket1Limit * HrmsConstants.Payroll.TaxRate1
                    + (annualTaxableIncome - HrmsConstants.Payroll.TaxBracket1Limit) * HrmsConstants.Payroll.TaxRate2;
            }
            else if (annualTaxableIncome <= HrmsConstants.Payroll.TaxBracket3Limit)
            {
                tax = HrmsConstants.Payroll.TaxBracket1Limit * HrmsConstants.Payroll.TaxRate1
                    + (HrmsConstants.Payroll.TaxBracket2Limit - HrmsConstants.Payroll.TaxBracket1Limit) * HrmsConstants.Payroll.TaxRate2
                    + (annualTaxableIncome - HrmsConstants.Payroll.TaxBracket2Limit) * HrmsConstants.Payroll.TaxRate3;
            }
            else
            {
                tax = HrmsConstants.Payroll.TaxBracket1Limit * HrmsConstants.Payroll.TaxRate1
                    + (HrmsConstants.Payroll.TaxBracket2Limit - HrmsConstants.Payroll.TaxBracket1Limit) * HrmsConstants.Payroll.TaxRate2
                    + (HrmsConstants.Payroll.TaxBracket3Limit - HrmsConstants.Payroll.TaxBracket2Limit) * HrmsConstants.Payroll.TaxRate3
                    + (annualTaxableIncome - HrmsConstants.Payroll.TaxBracket3Limit) * HrmsConstants.Payroll.TaxRate4;
            }

            return Math.Round(tax, 2);
        }

        // ── Private helpers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Computes all payroll components from an employee's monthly gross salary and
        /// the per-period adjustments in <paramref name="dto"/>.
        /// </summary>
        private Core.Entities.Payroll CalculatePayroll(decimal annualSalary, ProcessPayrollDto dto)
        {
            var monthly = annualSalary / HrmsConstants.Payroll.MonthsPerYear;
            var breakdown = ComputeSalaryBreakdown(annualSalary);

            // Loss of pay deduction
            decimal lopDeduction = dto.LopDays > 0 && dto.WorkingDays > 0
                ? Math.Round(breakdown.GrossSalary / dto.WorkingDays * dto.LopDays, 2)
                : 0m;

            var grossAfterLop = breakdown.GrossSalary - lopDeduction;
            var pfDeduction = Math.Round(breakdown.BasicSalary * HrmsConstants.Payroll.ProvidentFundRate, 2);
            var esiDeduction = grossAfterLop <= HrmsConstants.Payroll.EsiEligibilityLimit
                ? Math.Round(grossAfterLop * HrmsConstants.Payroll.EsiRate, 2)
                : 0m;
            var annualTaxable = (grossAfterLop - pfDeduction - esiDeduction) * HrmsConstants.Payroll.MonthsPerYear
                - HrmsConstants.Payroll.StandardDeductionAnnual;
            var monthlyTax = Math.Round(CalculateIncomeTax(Math.Max(annualTaxable, 0m)) / HrmsConstants.Payroll.MonthsPerYear, 2);
            var professionalTax = CalculateProfessionalTax(grossAfterLop);

            var totalDeductions = pfDeduction + esiDeduction + monthlyTax + professionalTax
                + dto.LoanDeduction + dto.OtherDeductions + lopDeduction;

            var netSalary = grossAfterLop + dto.OtherAllowances + dto.LeaveEncashment - totalDeductions;

            return new Core.Entities.Payroll
            {
                BasicSalary = breakdown.BasicSalary,
                HouseRentAllowance = breakdown.HouseRentAllowance,
                ConveyanceAllowance = breakdown.ConveyanceAllowance,
                MedicalAllowance = breakdown.MedicalAllowance,
                OtherAllowances = dto.OtherAllowances,
                GrossSalary = grossAfterLop + dto.OtherAllowances,
                ProvidentFund = pfDeduction,
                EmployeeStateInsurance = esiDeduction,
                IncomeTax = monthlyTax,
                ProfessionalTax = professionalTax,
                LoanDeduction = dto.LoanDeduction,
                OtherDeductions = dto.OtherDeductions,
                TotalDeductions = totalDeductions,
                NetSalary = Math.Round(netSalary, 2),
                EmployerProvidentFund = Math.Round(breakdown.BasicSalary * HrmsConstants.Payroll.EmployerPfRate, 2),
                EmployerEsi = grossAfterLop <= HrmsConstants.Payroll.EsiEligibilityLimit
                    ? Math.Round(grossAfterLop * HrmsConstants.Payroll.EmployerEsiRate, 2)
                    : 0m,
                WorkingDays = dto.WorkingDays,
                PaidDays = dto.PaidDays,
                LopDays = dto.LopDays,
                LopDeduction = lopDeduction,
                LeaveEncashment = dto.LeaveEncashment
            };
        }

        private SalaryBreakdownDto ComputeSalaryBreakdown(decimal annualSalary)
        {
            var monthly = annualSalary / HrmsConstants.Payroll.MonthsPerYear;
            var basic = Math.Round(monthly * 0.50m, 2);  // 50% of monthly CTC as basic
            var hra = Math.Round(basic * HrmsConstants.Payroll.HraRate, 2);
            var conveyance = HrmsConstants.Payroll.ConveyanceAllowance;
            var medical = HrmsConstants.Payroll.MedicalAllowance;
            var gross = basic + hra + conveyance + medical;

            var pf = Math.Round(basic * HrmsConstants.Payroll.ProvidentFundRate, 2);
            var esi = gross <= HrmsConstants.Payroll.EsiEligibilityLimit
                ? Math.Round(gross * HrmsConstants.Payroll.EsiRate, 2)
                : 0m;
            var annualTaxable = (gross - pf - esi) * HrmsConstants.Payroll.MonthsPerYear
                - HrmsConstants.Payroll.StandardDeductionAnnual;
            var monthlyTax = Math.Round(CalculateIncomeTax(Math.Max(annualTaxable, 0m)) / HrmsConstants.Payroll.MonthsPerYear, 2);
            var pt = CalculateProfessionalTax(gross);
            var totalDed = pf + esi + monthlyTax + pt;

            return new SalaryBreakdownDto
            {
                AnnualSalary = annualSalary,
                MonthlySalary = Math.Round(monthly, 2),
                BasicSalary = basic,
                HouseRentAllowance = hra,
                ConveyanceAllowance = conveyance,
                MedicalAllowance = medical,
                GrossSalary = gross,
                ProvidentFund = pf,
                EmployeeStateInsurance = esi,
                EstimatedIncomeTax = monthlyTax,
                ProfessionalTax = pt,
                TotalDeductions = totalDed,
                NetSalary = Math.Round(gross - totalDed, 2)
            };
        }

        /// <summary>
        /// Returns professional tax based on monthly gross (simple slab for illustration).
        /// </summary>
        private static decimal CalculateProfessionalTax(decimal monthlyGross)
        {
            return monthlyGross switch
            {
                <= 7500m => 0m,
                <= 10000m => 175m,
                _ => 200m
            };
        }

        private void InvalidatePayrollCache(int payrollId, int employeeId, int year, int month)
        {
            _cache.Remove(HrmsConstants.Payroll.PayrollKey(payrollId));
            _cache.Remove(HrmsConstants.Payroll.EmployeePayrollKey(employeeId, year, month));
        }
    }
}
