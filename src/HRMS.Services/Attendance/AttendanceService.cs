using AutoMapper;
using HRMS.Core.Entities;
using HRMS.Core.Enums;
using HRMS.Core.Interfaces.Repositories;
using HRMS.Core.Interfaces.Services;
using HRMS.Services.Attendance.Dtos;
using HRMS.Shared.Constants;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace HRMS.Services.Attendance
{
    /// <summary>
    /// Service for managing employee attendance records including check-in/out and reporting.
    /// </summary>
    public class AttendanceService : IAttendanceService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<AttendanceService> _logger;
        private readonly IMemoryCache _cache;
        private readonly IDateTimeProvider _dateTimeProvider;

        public AttendanceService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<AttendanceService> logger,
            IMemoryCache cache,
            IDateTimeProvider dateTimeProvider)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _cache = cache;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<AttendanceDto?> GetAttendanceByIdAsync(int id)
        {
            try
            {
                var cacheKey = HrmsConstants.Cache.AttendanceKey(id);
                if (_cache.TryGetValue(cacheKey, out AttendanceDto? cached))
                    return cached;

                var attendance = await _unitOfWork.Attendances.GetByIdAsync(id);
                if (attendance == null) return null;

                var dto = _mapper.Map<AttendanceDto>(attendance);
                _cache.Set(cacheKey, dto, TimeSpan.FromMinutes(HrmsConstants.Cache.DefaultExpirationMinutes));
                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting attendance with ID {AttendanceId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<AttendanceDto>> GetAttendanceByEmployeeAsync(int employeeId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var records = await _unitOfWork.Attendances.GetAttendanceByEmployeeAsync(employeeId, startDate, endDate);
                return _mapper.Map<IEnumerable<AttendanceDto>>(records);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting attendance for employee {EmployeeId}", employeeId);
                throw;
            }
        }

        public async Task<IEnumerable<AttendanceDto>> GetAttendanceByDateAsync(DateTime date)
        {
            try
            {
                var records = await _unitOfWork.Attendances.GetAttendanceByDateAsync(date);
                return _mapper.Map<IEnumerable<AttendanceDto>>(records);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting attendance for date {Date}", date);
                throw;
            }
        }

        public async Task<IEnumerable<AttendanceDto>> GetAttendanceByStatusAsync(int employeeId, AttendanceStatus status, int year, int month)
        {
            try
            {
                var records = await _unitOfWork.Attendances.GetAttendanceByStatusAsync(employeeId, status, year, month);
                return _mapper.Map<IEnumerable<AttendanceDto>>(records);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting attendance by status for employee {EmployeeId}", employeeId);
                throw;
            }
        }

        public async Task<AttendanceDto> CreateAttendanceAsync(CreateAttendanceDto createDto)
        {
            try
            {
                _logger.LogInformation("Creating attendance for employee {EmployeeId} on {Date}",
                    createDto.EmployeeId, createDto.Date);

                var existing = await _unitOfWork.Attendances.GetAttendanceByEmployeeAndDateAsync(
                    createDto.EmployeeId, createDto.Date);
                if (existing != null)
                    throw new InvalidOperationException(
                        $"Attendance record already exists for employee {createDto.EmployeeId} on {createDto.Date:yyyy-MM-dd}");

                var attendance = _mapper.Map<Core.Entities.Attendance>(createDto);
                attendance.CreatedAt = _dateTimeProvider.UtcNow;
                CalculateTotals(attendance);

                await _unitOfWork.Attendances.AddAsync(attendance);
                await _unitOfWork.CompleteAsync();

                _logger.LogInformation("Attendance created with ID {AttendanceId}", attendance.Id);
                InvalidateEmployeeAttendanceCache(createDto.EmployeeId);

                return _mapper.Map<AttendanceDto>(attendance);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating attendance");
                throw;
            }
        }

        public async Task<AttendanceDto> UpdateAttendanceAsync(UpdateAttendanceDto updateDto)
        {
            try
            {
                _logger.LogInformation("Updating attendance {AttendanceId}", updateDto.Id);

                var attendance = await _unitOfWork.Attendances.GetByIdAsync(updateDto.Id);
                if (attendance == null)
                    throw new KeyNotFoundException($"Attendance record with ID {updateDto.Id} not found");

                attendance.CheckInTime = updateDto.CheckInTime;
                attendance.CheckOutTime = updateDto.CheckOutTime;
                attendance.Status = updateDto.Status;
                attendance.Notes = updateDto.Notes;
                attendance.UpdatedAt = _dateTimeProvider.UtcNow;
                CalculateTotals(attendance);

                _unitOfWork.Attendances.Update(attendance);
                await _unitOfWork.CompleteAsync();

                _cache.Remove(HrmsConstants.Cache.AttendanceKey(attendance.Id));
                InvalidateEmployeeAttendanceCache(attendance.EmployeeId);

                return _mapper.Map<AttendanceDto>(attendance);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating attendance {AttendanceId}", updateDto.Id);
                throw;
            }
        }

        public async Task<AttendanceDto> CheckInAsync(CheckInDto checkInDto)
        {
            try
            {
                _logger.LogInformation("Employee {EmployeeId} checking in at {Time}",
                    checkInDto.EmployeeId, checkInDto.CheckInTime);

                var existing = await _unitOfWork.Attendances.GetAttendanceByEmployeeAndDateAsync(
                    checkInDto.EmployeeId, checkInDto.CheckInTime.Date);

                if (existing != null)
                {
                    if (existing.CheckInTime.HasValue)
                        throw new InvalidOperationException("Employee has already checked in today");

                    existing.CheckInTime = checkInDto.CheckInTime;
                    existing.Notes = checkInDto.Notes;
                    existing.UpdatedAt = _dateTimeProvider.UtcNow;
                    CalculateTotals(existing);
                    _unitOfWork.Attendances.Update(existing);
                    await _unitOfWork.CompleteAsync();
                    InvalidateEmployeeAttendanceCache(checkInDto.EmployeeId);
                    return _mapper.Map<AttendanceDto>(existing);
                }

                // Determine status based on check-in time
                var workStart = checkInDto.CheckInTime.Date.Add(TimeSpan.Parse(HrmsConstants.Attendance.DefaultWorkStartTime));
                var lateThreshold = workStart.AddMinutes(HrmsConstants.Attendance.LateThresholdMinutes);
                var status = checkInDto.CheckInTime > lateThreshold ? AttendanceStatus.Late : AttendanceStatus.Present;
                var lateMinutes = checkInDto.CheckInTime > lateThreshold
                    ? (decimal)(checkInDto.CheckInTime - lateThreshold).TotalMinutes
                    : 0m;

                var attendance = new Core.Entities.Attendance
                {
                    EmployeeId = checkInDto.EmployeeId,
                    Date = checkInDto.CheckInTime.Date,
                    CheckInTime = checkInDto.CheckInTime,
                    Status = status,
                    LateMinutes = lateMinutes,
                    Notes = checkInDto.Notes,
                    CreatedAt = _dateTimeProvider.UtcNow
                };

                await _unitOfWork.Attendances.AddAsync(attendance);
                await _unitOfWork.CompleteAsync();
                InvalidateEmployeeAttendanceCache(checkInDto.EmployeeId);

                return _mapper.Map<AttendanceDto>(attendance);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing check-in for employee {EmployeeId}", checkInDto.EmployeeId);
                throw;
            }
        }

        public async Task<AttendanceDto> CheckOutAsync(CheckOutDto checkOutDto)
        {
            try
            {
                _logger.LogInformation("Employee {EmployeeId} checking out at {Time}",
                    checkOutDto.EmployeeId, checkOutDto.CheckOutTime);

                var attendance = await _unitOfWork.Attendances.GetAttendanceByEmployeeAndDateAsync(
                    checkOutDto.EmployeeId, checkOutDto.CheckOutTime.Date);

                if (attendance == null || !attendance.CheckInTime.HasValue)
                    throw new InvalidOperationException("No check-in record found for today");

                if (attendance.CheckOutTime.HasValue)
                    throw new InvalidOperationException("Employee has already checked out today");

                attendance.CheckOutTime = checkOutDto.CheckOutTime;
                if (!string.IsNullOrEmpty(checkOutDto.Notes))
                    attendance.Notes = checkOutDto.Notes;
                attendance.UpdatedAt = _dateTimeProvider.UtcNow;
                CalculateTotals(attendance);

                _unitOfWork.Attendances.Update(attendance);
                await _unitOfWork.CompleteAsync();

                _cache.Remove(HrmsConstants.Cache.AttendanceKey(attendance.Id));
                InvalidateEmployeeAttendanceCache(checkOutDto.EmployeeId);

                return _mapper.Map<AttendanceDto>(attendance);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing check-out for employee {EmployeeId}", checkOutDto.EmployeeId);
                throw;
            }
        }

        public async Task<bool> DeleteAttendanceAsync(int id)
        {
            try
            {
                var attendance = await _unitOfWork.Attendances.GetByIdAsync(id);
                if (attendance == null)
                    throw new KeyNotFoundException($"Attendance record with ID {id} not found");

                attendance.IsDeleted = true;
                attendance.UpdatedAt = _dateTimeProvider.UtcNow;
                _unitOfWork.Attendances.Update(attendance);
                await _unitOfWork.CompleteAsync();

                _cache.Remove(HrmsConstants.Cache.AttendanceKey(id));
                InvalidateEmployeeAttendanceCache(attendance.EmployeeId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting attendance {AttendanceId}", id);
                throw;
            }
        }

        public async Task<AttendanceSummaryDto> GetMonthlySummaryAsync(int employeeId, int year, int month)
        {
            try
            {
                var startDate = new DateTime(year, month, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);

                var records = (await _unitOfWork.Attendances.GetAttendanceByEmployeeAsync(
                    employeeId, startDate, endDate)).ToList();

                var employee = await _unitOfWork.Employees.GetByIdAsync(employeeId);

                var summary = new AttendanceSummaryDto
                {
                    EmployeeId = employeeId,
                    EmployeeName = employee?.FullName ?? string.Empty,
                    Year = year,
                    Month = month,
                    TotalDays = records.Count,
                    PresentDays = records.Count(r => r.Status == AttendanceStatus.Present),
                    AbsentDays = records.Count(r => r.Status == AttendanceStatus.Absent),
                    LateDays = records.Count(r => r.Status == AttendanceStatus.Late),
                    HalfDays = records.Count(r => r.Status == AttendanceStatus.HalfDay),
                    OnLeaveDays = records.Count(r => r.Status == AttendanceStatus.OnLeave),
                    TotalOvertimeHours = records.Sum(r => r.OvertimeHours),
                    AttendancePercentage = await _unitOfWork.Attendances.GetAttendancePercentageAsync(
                        employeeId, startDate, endDate)
                };

                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting monthly summary for employee {EmployeeId}", employeeId);
                throw;
            }
        }

        public async Task<decimal> GetTotalOvertimeHoursAsync(int employeeId, DateTime startDate, DateTime endDate)
        {
            return await _unitOfWork.Attendances.GetTotalOvertimeHoursAsync(employeeId, startDate, endDate);
        }

        public async Task<double> GetAttendancePercentageAsync(int employeeId, DateTime startDate, DateTime endDate)
        {
            return await _unitOfWork.Attendances.GetAttendancePercentageAsync(employeeId, startDate, endDate);
        }

        public async Task<bool> HasCheckedInTodayAsync(int employeeId)
        {
            return await _unitOfWork.Attendances.HasCheckedInTodayAsync(employeeId, _dateTimeProvider.UtcNow);
        }

        // --- Helpers ---

        private static void CalculateTotals(Core.Entities.Attendance attendance)
        {
            if (attendance.CheckInTime.HasValue && attendance.CheckOutTime.HasValue)
            {
                var worked = attendance.CheckOutTime.Value - attendance.CheckInTime.Value;
                attendance.TotalHours = worked;

                var standardHours = TimeSpan.FromHours(HrmsConstants.Attendance.StandardWorkHours);
                var overtimeThreshold = standardHours.Add(TimeSpan.FromMinutes(HrmsConstants.Attendance.OvertimeThresholdMinutes));

                if (worked > overtimeThreshold)
                    attendance.OvertimeHours = (decimal)(worked - standardHours).TotalHours;
                else
                    attendance.OvertimeHours = 0m;
            }
        }

        private void InvalidateEmployeeAttendanceCache(int employeeId)
        {
            _cache.Remove(HrmsConstants.Cache.EmployeeAttendanceKey(employeeId));
        }
    }
}
