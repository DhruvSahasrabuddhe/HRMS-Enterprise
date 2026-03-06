using AutoMapper;
using FluentValidation;
using HRMS.Core.Entities;
using HRMS.Core.Enums;
using HRMS.Core.Interfaces.Repositories;
using HRMS.Core.Interfaces.Services;
using HRMS.Services.Employees.Dtos;
using HRMS.Shared.Constants;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace HRMS.Services.Employees
{
    /// <summary>
    /// Service for managing employee operations including CRUD, search, and import/export.
    /// </summary>
    public class EmployeeService : IEmployeeService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<EmployeeService> _logger;
        private readonly IMemoryCache _cache;
        private readonly IValidator<CreateEmployeeDto> _createValidator;
        private readonly IValidator<UpdateEmployeeDto> _updateValidator;
        private readonly IEmployeeCodeGenerator _codeGenerator;
        private readonly IDateTimeProvider _dateTimeProvider;

        public EmployeeService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<EmployeeService> logger,
            IMemoryCache cache,
            IValidator<CreateEmployeeDto> createValidator,
            IValidator<UpdateEmployeeDto> updateValidator,
            IEmployeeCodeGenerator codeGenerator,
            IDateTimeProvider dateTimeProvider)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _cache = cache;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _codeGenerator = codeGenerator;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<EmployeeDto?> GetEmployeeByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("Getting employee with ID: {EmployeeId}", id);

                // Try to get from cache first
                var cacheKey = HrmsConstants.Cache.EmployeeKey(id);
                if (_cache.TryGetValue(cacheKey, out EmployeeDto? cachedEmployee))
                {
                    _logger.LogDebug("Employee {EmployeeId} found in cache", id);
                    return cachedEmployee;
                }

                var employee = await _unitOfWork.Employees.GetEmployeeWithDetailsAsync(id);
                if (employee == null)
                {
                    _logger.LogWarning("Employee with ID {EmployeeId} not found", id);
                    return null;
                }

                var employeeDto = _mapper.Map<EmployeeDto>(employee);

                // Store in cache with default expiration
                _cache.Set(cacheKey, employeeDto, TimeSpan.FromMinutes(HrmsConstants.Cache.DefaultExpirationMinutes));

                return employeeDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting employee with ID {EmployeeId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<EmployeeListDto>> GetAllEmployeesAsync()
        {
            try
            {
                _logger.LogInformation("Getting all employees");

                var employees = await _unitOfWork.Employees.GetAllAsync();
                return _mapper.Map<IEnumerable<EmployeeListDto>>(employees);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all employees");
                throw;
            }
        }

        public async Task<IEnumerable<EmployeeListDto>> SearchEmployeesAsync(EmployeeSearchDto searchDto)
        {
            try
            {
                _logger.LogInformation("Searching employees with term: {SearchTerm}", searchDto.SearchTerm);

                IEnumerable<Employee> employees;

                if (!string.IsNullOrWhiteSpace(searchDto.SearchTerm))
                {
                    employees = await _unitOfWork.Employees.SearchEmployeesAsync(searchDto.SearchTerm);
                }
                else if (searchDto.DepartmentId.HasValue)
                {
                    employees = await _unitOfWork.Employees.GetEmployeesByDepartmentAsync(searchDto.DepartmentId.Value);
                }
                else if (searchDto.ManagerId.HasValue)
                {
                    employees = await _unitOfWork.Employees.GetEmployeesByManagerAsync(searchDto.ManagerId.Value);
                }
                else
                {
                    employees = await _unitOfWork.Employees.GetAllAsync();
                }

                // Apply status filter
                if (searchDto.Status.HasValue)
                {
                    employees = employees.Where(e => e.Status == searchDto.Status.Value);
                }

                // Apply sorting
                employees = ApplySorting(employees, searchDto);

                // Apply pagination
                employees = employees
                    .Skip((searchDto.PageNumber - 1) * searchDto.PageSize)
                    .Take(searchDto.PageSize);

                return _mapper.Map<IEnumerable<EmployeeListDto>>(employees);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching employees");
                throw;
            }
        }

        public async Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeDto createDto)
        {
            try
            {
                _logger.LogInformation("Creating new employee: {FirstName} {LastName}",
                    createDto.FirstName, createDto.LastName);

                // Validate
                var validationResult = await _createValidator.ValidateAsync(createDto);
                if (!validationResult.IsValid)
                {
                    throw new ValidationException(validationResult.Errors);
                }

                // Check unique email
                var isEmailUnique = await _unitOfWork.Employees.IsEmailUniqueAsync(createDto.Email);
                if (!isEmailUnique)
                {
                    throw new InvalidOperationException($"Email {createDto.Email} is already in use");
                }

                // Generate employee code using the code generator service
                var employeeCode = await _codeGenerator.GenerateEmployeeCodeAsync();

                var employee = _mapper.Map<Employee>(createDto);
                employee.EmployeeCode = employeeCode;
                employee.Status = EmployeeStatus.Active;
                employee.CreatedAt = _dateTimeProvider.UtcNow;

                await _unitOfWork.Employees.AddAsync(employee);
                await _unitOfWork.CompleteAsync();

                _logger.LogInformation("Employee created successfully with ID: {EmployeeId}, Code: {EmployeeCode}",
                    employee.Id, employee.EmployeeCode);

                // Clear cache
                _cache.Remove(HrmsConstants.Cache.EmployeeListKey);

                return _mapper.Map<EmployeeDto>(employee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating employee");
                throw;
            }
        }

        public async Task<EmployeeDto> UpdateEmployeeAsync(UpdateEmployeeDto updateDto)
        {
            try
            {
                _logger.LogInformation("Updating employee with ID: {EmployeeId}", updateDto.Id);

                // Validate
                var validationResult = await _updateValidator.ValidateAsync(updateDto);
                if (!validationResult.IsValid)
                {
                    throw new ValidationException(validationResult.Errors);
                }

                var employee = await _unitOfWork.Employees.GetByIdAsync(updateDto.Id);
                if (employee == null)
                {
                    throw new KeyNotFoundException($"Employee with ID {updateDto.Id} not found");
                }

                // Check unique email if changed
                if (employee.Email != updateDto.Email)
                {
                    var isEmailUnique = await _unitOfWork.Employees.IsEmailUniqueAsync(updateDto.Email, updateDto.Id);
                    if (!isEmailUnique)
                    {
                        throw new InvalidOperationException($"Email {updateDto.Email} is already in use");
                    }
                }

                _mapper.Map(updateDto, employee);
                employee.UpdatedAt = _dateTimeProvider.UtcNow;

                _unitOfWork.Employees.Update(employee);
                await _unitOfWork.CompleteAsync();

                _logger.LogInformation("Employee {EmployeeId} updated successfully", employee.Id);

                // Clear cache
                _cache.Remove(HrmsConstants.Cache.EmployeeKey(employee.Id));
                _cache.Remove(HrmsConstants.Cache.EmployeeListKey);

                return _mapper.Map<EmployeeDto>(employee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating employee with ID {EmployeeId}", updateDto.Id);
                throw;
            }
        }

        public async Task<bool> DeleteEmployeeAsync(int id)
        {
            try
            {
                _logger.LogInformation("Soft deleting employee with ID: {EmployeeId}", id);

                var employee = await _unitOfWork.Employees.GetByIdAsync(id);
                if (employee == null)
                {
                    throw new KeyNotFoundException($"Employee with ID {id} not found");
                }

                // Soft delete
                employee.IsDeleted = true;
                employee.Status = EmployeeStatus.Terminated;
                employee.UpdatedAt = _dateTimeProvider.UtcNow;

                _unitOfWork.Employees.Update(employee);
                await _unitOfWork.CompleteAsync();

                _logger.LogInformation("Employee {EmployeeId} deleted successfully", id);

                // Clear cache
                _cache.Remove(HrmsConstants.Cache.EmployeeKey(id));
                _cache.Remove(HrmsConstants.Cache.EmployeeListKey);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting employee with ID {EmployeeId}", id);
                throw;
            }
        }

        public async Task<bool> IsEmailUniqueAsync(string email, int? excludeId = null)
        {
            return await _unitOfWork.Employees.IsEmailUniqueAsync(email, excludeId);
        }

        public async Task<bool> IsEmployeeCodeUniqueAsync(string code, int? excludeId = null)
        {
            return await _unitOfWork.Employees.IsEmployeeCodeUniqueAsync(code, excludeId);
        }

        public async Task<int> GetEmployeeCountAsync()
        {
            return await _unitOfWork.Employees.CountAsync(e => !e.IsDeleted);
        }

        public Task<byte[]> ExportEmployeesToExcelAsync(EmployeeSearchDto searchDto)
        {
            _logger.LogWarning("Excel export not implemented yet");
            throw new NotImplementedException("Excel export functionality is not yet implemented. Please use a library like EPPlus or ClosedXML.");
        }

        public Task<int> ImportEmployeesFromExcelAsync(byte[] fileData)
        {
            _logger.LogWarning("Excel import not implemented yet");
            throw new NotImplementedException("Excel import functionality is not yet implemented. Please use a library like EPPlus or ClosedXML.");
        }

        public async Task<IEnumerable<EmployeeListDto>> GetEmployeesByDepartmentAsync(int departmentId)
        {
            var employees = await _unitOfWork.Employees.GetEmployeesByDepartmentAsync(departmentId);
            return _mapper.Map<IEnumerable<EmployeeListDto>>(employees);
        }

        public async Task<IEnumerable<EmployeeListDto>> GetEmployeesByManagerAsync(int managerId)
        {
            var employees = await _unitOfWork.Employees.GetEmployeesByManagerAsync(managerId);
            return _mapper.Map<IEnumerable<EmployeeListDto>>(employees);
        }



        private IEnumerable<Employee> ApplySorting(IEnumerable<Employee> employees, EmployeeSearchDto searchDto)
        {
            if (employees == null || !employees.Any())
                return Enumerable.Empty<Employee>();

            return searchDto.SortBy?.ToLower() switch
            {
                "firstname" => searchDto.SortAscending
                    ? employees.OrderBy(e => e.FirstName)
                    : employees.OrderByDescending(e => e.FirstName),
                "lastname" => searchDto.SortAscending
                    ? employees.OrderBy(e => e.LastName)
                    : employees.OrderByDescending(e => e.LastName),
                "hiredate" => searchDto.SortAscending
                    ? employees.OrderBy(e => e.HireDate)
                    : employees.OrderByDescending(e => e.HireDate),
                "department" => searchDto.SortAscending
                    ? employees.OrderBy(e => e.Department != null ? e.Department.Name : "")
                    : employees.OrderByDescending(e => e.Department != null ? e.Department.Name : ""),
                _ => searchDto.SortAscending
                    ? employees.OrderBy(e => e.LastName)
                    : employees.OrderByDescending(e => e.LastName)
            };
        }
    }
}