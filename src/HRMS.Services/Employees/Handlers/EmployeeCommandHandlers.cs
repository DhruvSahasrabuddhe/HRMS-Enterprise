using AutoMapper;
using FluentValidation;
using HRMS.Core.CQRS;
using HRMS.Core.Entities;
using HRMS.Core.Enums;
using HRMS.Core.Events;
using HRMS.Core.Interfaces.Repositories;
using HRMS.Services.Employees.Commands;
using HRMS.Services.Employees.Dtos;
using HRMS.Shared.Common;
using Microsoft.Extensions.Logging;

namespace HRMS.Services.Employees.Handlers
{
    /// <summary>
    /// Handles <see cref="CreateEmployeeCommand"/>.
    /// Encapsulates all create-employee business rules in a single, focused handler.
    /// </summary>
    public sealed class CreateEmployeeCommandHandler : ICommandHandler<CreateEmployeeCommand, EmployeeDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<CreateEmployeeCommandHandler> _logger;
        private readonly IValidator<CreateEmployeeDto> _validator;

        public CreateEmployeeCommandHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<CreateEmployeeCommandHandler> logger,
            IValidator<CreateEmployeeDto> validator)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _validator = validator;
        }

        public async Task<Result<EmployeeDto>> HandleAsync(
            CreateEmployeeCommand command,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Handling CreateEmployeeCommand for {FirstName} {LastName}",
                command.FirstName, command.LastName);

            // Map command → validation DTO and validate
            var createDto = _mapper.Map<CreateEmployeeDto>(command);
            var validationResult = await _validator.ValidateAsync(createDto, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                return Result.Failure<EmployeeDto>(errors);
            }

            // Business rule: email must be unique
            if (!await _unitOfWork.Employees.IsEmailUniqueAsync(command.Email))
                return Result.Failure<EmployeeDto>($"Email '{command.Email}' is already in use.");

            // Generate employee code
            var count = await _unitOfWork.Employees.CountAsync(_ => true);
            var employeeCode = $"EMP{(count + 1):D5}";

            var employee = _mapper.Map<Employee>(createDto);
            employee.EmployeeCode = employeeCode;
            employee.Status = EmployeeStatus.Active;

            // Raise domain event
            employee.AddDomainEvent(new EmployeeCreatedEvent(
                employee.Id, employee.EmployeeCode, employee.FullName, employee.DepartmentId));

            await _unitOfWork.Employees.AddAsync(employee);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Employee created: {EmployeeCode}", employee.EmployeeCode);
            return Result.Success(_mapper.Map<EmployeeDto>(employee));
        }
    }

    /// <summary>Handles <see cref="UpdateEmployeeCommand"/>.</summary>
    public sealed class UpdateEmployeeCommandHandler : ICommandHandler<UpdateEmployeeCommand, EmployeeDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<UpdateEmployeeCommandHandler> _logger;
        private readonly IValidator<UpdateEmployeeDto> _validator;

        public UpdateEmployeeCommandHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<UpdateEmployeeCommandHandler> logger,
            IValidator<UpdateEmployeeDto> validator)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _validator = validator;
        }

        public async Task<Result<EmployeeDto>> HandleAsync(
            UpdateEmployeeCommand command,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Handling UpdateEmployeeCommand for ID {EmployeeId}", command.Id);

            var updateDto = _mapper.Map<UpdateEmployeeDto>(command);
            var validationResult = await _validator.ValidateAsync(updateDto, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                return Result.Failure<EmployeeDto>(errors);
            }

            var employee = await _unitOfWork.Employees.GetByIdAsync(command.Id);
            if (employee is null)
                return Result.NotFound<EmployeeDto>("Employee", command.Id);

            // Track department change for domain event
            var oldDepartmentId = employee.DepartmentId;

            _mapper.Map(updateDto, employee);

            if (oldDepartmentId != employee.DepartmentId)
            {
                employee.AddDomainEvent(new EmployeeDepartmentChangedEvent(
                    employee.Id, oldDepartmentId, employee.DepartmentId));
            }

            _unitOfWork.Employees.Update(employee);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Employee {EmployeeId} updated successfully", employee.Id);
            return Result.Success(_mapper.Map<EmployeeDto>(employee));
        }
    }

    /// <summary>Handles <see cref="DeleteEmployeeCommand"/>.</summary>
    public sealed class DeleteEmployeeCommandHandler : ICommandHandler<DeleteEmployeeCommand>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeleteEmployeeCommandHandler> _logger;

        public DeleteEmployeeCommandHandler(IUnitOfWork unitOfWork, ILogger<DeleteEmployeeCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<Unit>> HandleAsync(
            DeleteEmployeeCommand command,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Handling DeleteEmployeeCommand for ID {EmployeeId}", command.EmployeeId);

            var employee = await _unitOfWork.Employees.GetByIdAsync(command.EmployeeId);
            if (employee is null)
                return Result.NotFound<Unit>("Employee", command.EmployeeId);

            _unitOfWork.Employees.Remove(employee);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Employee {EmployeeId} deleted", command.EmployeeId);
            return Result.Success(Unit.Value);
        }
    }
}
