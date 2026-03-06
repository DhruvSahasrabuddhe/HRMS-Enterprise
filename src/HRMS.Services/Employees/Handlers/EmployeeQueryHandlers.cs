using AutoMapper;
using HRMS.Core.CQRS;
using HRMS.Core.Interfaces.Repositories;
using HRMS.Services.Employees.Dtos;
using HRMS.Services.Employees.Queries;
using HRMS.Shared.Common;
using Microsoft.Extensions.Logging;

namespace HRMS.Services.Employees.Handlers
{
    /// <summary>
    /// Handles <see cref="GetEmployeeByIdQuery"/>.
    /// Read-only handler – never modifies state.
    /// </summary>
    public sealed class GetEmployeeByIdQueryHandler : IQueryHandler<GetEmployeeByIdQuery, EmployeeDto?>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<GetEmployeeByIdQueryHandler> _logger;

        public GetEmployeeByIdQueryHandler(
            IUnitOfWork unitOfWork, IMapper mapper, ILogger<GetEmployeeByIdQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<EmployeeDto?>> HandleAsync(
            GetEmployeeByIdQuery query, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Handling GetEmployeeByIdQuery for ID {EmployeeId}", query.EmployeeId);

            var employee = await _unitOfWork.Employees.GetEmployeeWithDetailsAsync(query.EmployeeId);
            return Result.Success(_mapper.Map<EmployeeDto?>(employee));
        }
    }

    /// <summary>Handles <see cref="GetAllEmployeesQuery"/>.</summary>
    public sealed class GetAllEmployeesQueryHandler : IQueryHandler<GetAllEmployeesQuery, IEnumerable<EmployeeListDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<GetAllEmployeesQueryHandler> _logger;

        public GetAllEmployeesQueryHandler(
            IUnitOfWork unitOfWork, IMapper mapper, ILogger<GetAllEmployeesQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<IEnumerable<EmployeeListDto>>> HandleAsync(
            GetAllEmployeesQuery query, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Handling GetAllEmployeesQuery");

            var employees = await _unitOfWork.Employees.GetAllAsync();
            return Result.Success(_mapper.Map<IEnumerable<EmployeeListDto>>(employees));
        }
    }

    /// <summary>Handles <see cref="SearchEmployeesQuery"/>.</summary>
    public sealed class SearchEmployeesQueryHandler : IQueryHandler<SearchEmployeesQuery, IEnumerable<EmployeeListDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<SearchEmployeesQueryHandler> _logger;

        public SearchEmployeesQueryHandler(
            IUnitOfWork unitOfWork, IMapper mapper, ILogger<SearchEmployeesQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<IEnumerable<EmployeeListDto>>> HandleAsync(
            SearchEmployeesQuery query, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Handling SearchEmployeesQuery: term={Term}", query.SearchTerm);

            var employees = !string.IsNullOrWhiteSpace(query.SearchTerm)
                ? await _unitOfWork.Employees.SearchEmployeesAsync(query.SearchTerm)
                : query.DepartmentId.HasValue
                    ? await _unitOfWork.Employees.GetEmployeesByDepartmentAsync(query.DepartmentId.Value)
                    : query.ManagerId.HasValue
                        ? await _unitOfWork.Employees.GetEmployeesByManagerAsync(query.ManagerId.Value)
                        : await _unitOfWork.Employees.GetAllAsync();

            if (query.Status.HasValue)
                employees = employees.Where(e => e.Status == query.Status.Value);

            // Pagination
            employees = employees
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize);

            return Result.Success(_mapper.Map<IEnumerable<EmployeeListDto>>(employees));
        }
    }
}
