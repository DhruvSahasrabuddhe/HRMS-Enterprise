using AutoMapper;
using FluentValidation;
using HRMS.Core.Entities;
using HRMS.Core.Interfaces.Repositories;
using HRMS.Services.Departments.Dtos;
using HRMS.Shared.Constants;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace HRMS.Services.Departments
{
    public class DepartmentService : IDepartmentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<DepartmentService> _logger;
        private readonly IMemoryCache _cache;
        private readonly IValidator<CreateDepartmentDto> _createValidator;
        private readonly IValidator<UpdateDepartmentDto> _updateValidator;

        public DepartmentService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<DepartmentService> logger,
            IMemoryCache cache,
            IValidator<CreateDepartmentDto> createValidator,
            IValidator<UpdateDepartmentDto> updateValidator)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _cache = cache;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        public async Task<DepartmentDto?> GetDepartmentByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("Getting department with ID: {DepartmentId}", id);

                // Try cache first
                var cacheKey = HrmsConstants.Cache.DepartmentKey(id);
                if (_cache.TryGetValue(cacheKey, out DepartmentDto? cachedDepartment))
                {
                    return cachedDepartment;
                }

                var department = await _unitOfWork.Departments.GetDepartmentWithManagerAsync(id);
                if (department == null)
                {
                    _logger.LogWarning("Department with ID {DepartmentId} not found", id);
                    return null;
                }

                var departmentDto = _mapper.Map<DepartmentDto>(department);

                _cache.Set(cacheKey, departmentDto, TimeSpan.FromMinutes(HrmsConstants.Cache.DefaultExpirationMinutes));

                return departmentDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting department with ID {DepartmentId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<DepartmentListDto>> GetAllDepartmentsAsync()
        {
            try
            {
                _logger.LogInformation("Getting all departments");

                // Try cache first
                const string cacheKey = HrmsConstants.Cache.AllDepartmentsKey;
                if (_cache.TryGetValue(cacheKey, out IEnumerable<DepartmentListDto>? cachedDepartments))
                {
                    return cachedDepartments ?? Enumerable.Empty<DepartmentListDto>();
                }

                var departments = await _unitOfWork.Departments.GetDepartmentsWithEmployeeCountAsync();
                var departmentDtos = _mapper.Map<IEnumerable<DepartmentListDto>>(departments);

                _cache.Set(cacheKey, departmentDtos, TimeSpan.FromMinutes(HrmsConstants.Cache.DefaultExpirationMinutes));

                return departmentDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all departments");
                throw;
            }
        }

        public async Task<DepartmentDetailDto?> GetDepartmentWithEmployeesAsync(int id)
        {
            try
            {
                _logger.LogInformation("Getting department with employees for ID: {DepartmentId}", id);

                var department = await _unitOfWork.Departments.GetDepartmentWithEmployeesAsync(id);
                if (department == null)
                {
                    _logger.LogWarning("Department with ID {DepartmentId} not found", id);
                    return null;
                }

                return _mapper.Map<DepartmentDetailDto>(department);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting department with employees for ID {DepartmentId}", id);
                throw;
            }
        }

        public async Task<DepartmentDto> CreateDepartmentAsync(CreateDepartmentDto createDto)
        {
            try
            {
                _logger.LogInformation("Creating new department: {DepartmentName}", createDto.Name);

                // Validate
                var validationResult = await _createValidator.ValidateAsync(createDto);
                if (!validationResult.IsValid)
                {
                    throw new ValidationException(validationResult.Errors);
                }

                // Check unique code
                var isCodeUnique = await _unitOfWork.Departments.IsDepartmentCodeUniqueAsync(createDto.Code);
                if (!isCodeUnique)
                {
                    throw new InvalidOperationException($"Department code {createDto.Code} is already in use");
                }

                // Check unique name
                var isNameUnique = await _unitOfWork.Departments.IsDepartmentNameUniqueAsync(createDto.Name);
                if (!isNameUnique)
                {
                    throw new InvalidOperationException($"Department name {createDto.Name} is already in use");
                }

                var department = _mapper.Map<Department>(createDto);
                department.CreatedAt = DateTime.UtcNow;

                await _unitOfWork.Departments.AddAsync(department);
                await _unitOfWork.CompleteAsync();

                _logger.LogInformation("Department created successfully with ID: {DepartmentId}", department.Id);

                // Clear cache
                _cache.Remove(HrmsConstants.Cache.AllDepartmentsKey);

                return _mapper.Map<DepartmentDto>(department);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating department");
                throw;
            }
        }

        public async Task<DepartmentDto> UpdateDepartmentAsync(UpdateDepartmentDto updateDto)
        {
            try
            {
                _logger.LogInformation("Updating department with ID: {DepartmentId}", updateDto.Id);

                // Validate
                var validationResult = await _updateValidator.ValidateAsync(updateDto);
                if (!validationResult.IsValid)
                {
                    throw new ValidationException(validationResult.Errors);
                }

                var department = await _unitOfWork.Departments.GetByIdAsync(updateDto.Id);
                if (department == null)
                {
                    throw new KeyNotFoundException($"Department with ID {updateDto.Id} not found");
                }

                // Check unique code if changed
                if (department.Code != updateDto.Code)
                {
                    var isCodeUnique = await _unitOfWork.Departments.IsDepartmentCodeUniqueAsync(updateDto.Code, updateDto.Id);
                    if (!isCodeUnique)
                    {
                        throw new InvalidOperationException($"Department code {updateDto.Code} is already in use");
                    }
                }

                // Check unique name if changed
                if (department.Name != updateDto.Name)
                {
                    var isNameUnique = await _unitOfWork.Departments.IsDepartmentNameUniqueAsync(updateDto.Name, updateDto.Id);
                    if (!isNameUnique)
                    {
                        throw new InvalidOperationException($"Department name {updateDto.Name} is already in use");
                    }
                }

                _mapper.Map(updateDto, department);
                department.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Departments.Update(department);
                await _unitOfWork.CompleteAsync();

                _logger.LogInformation("Department {DepartmentId} updated successfully", department.Id);

                // Clear cache
                _cache.Remove(HrmsConstants.Cache.DepartmentKey(department.Id));
                _cache.Remove(HrmsConstants.Cache.AllDepartmentsKey);

                return _mapper.Map<DepartmentDto>(department);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating department with ID {DepartmentId}", updateDto.Id);
                throw;
            }
        }

        public async Task<bool> DeleteDepartmentAsync(int id)
        {
            try
            {
                _logger.LogInformation("Deleting department with ID: {DepartmentId}", id);

                var department = await _unitOfWork.Departments.GetDepartmentWithEmployeesAsync(id);
                if (department == null)
                {
                    throw new KeyNotFoundException($"Department with ID {id} not found");
                }

                // Check if department has employees
                if (department.Employees != null && department.Employees.Any())
                {
                    _logger.LogWarning("Cannot delete department {DepartmentId} because it has employees", id);
                    return false;
                }

                // Soft delete
                department.IsDeleted = true;
                department.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Departments.Update(department);
                await _unitOfWork.CompleteAsync();

                _logger.LogInformation("Department {DepartmentId} deleted successfully", id);

                // Clear cache
                _cache.Remove(HrmsConstants.Cache.DepartmentKey(id));
                _cache.Remove(HrmsConstants.Cache.AllDepartmentsKey);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting department with ID {DepartmentId}", id);
                throw;
            }
        }

        public async Task<bool> IsDepartmentCodeUniqueAsync(string code, int? excludeId = null)
        {
            return await _unitOfWork.Departments.IsDepartmentCodeUniqueAsync(code, excludeId);
        }

        public async Task<bool> IsDepartmentNameUniqueAsync(string name, int? excludeId = null)
        {
            return await _unitOfWork.Departments.IsDepartmentNameUniqueAsync(name, excludeId);
        }

        public async Task<int> GetDepartmentCountAsync()
        {
            return await _unitOfWork.Departments.CountAsync(d => !d.IsDeleted);
        }

        public async Task<Dictionary<string, int>> GetEmployeeCountByDepartmentAsync()
        {
            var departments = await _unitOfWork.Departments.GetDepartmentsWithEmployeeCountAsync();
            return departments.ToDictionary(
                d => d.Name,
                d => d.Employees?.Count ?? 0
            );
        }
    }
}