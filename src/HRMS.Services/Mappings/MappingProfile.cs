using AutoMapper;
using HRMS.Core.Entities;
using HRMS.Core.Enums;
using HRMS.Services.Attendance.Dtos;
using HRMS.Services.Departments.Dtos;
using HRMS.Services.Employees.Commands;
using HRMS.Services.Employees.Dtos;
using HRMS.Services.Leave.Dtos;
using HRMS.Services.Payroll.Dtos;
using HRMS.Services.PerformanceReviews.Dtos;

namespace HRMS.Services.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Employee mappings
            CreateMap<Employee, EmployeeDto>()
                .ForMember(dest => dest.DepartmentName,
                    opt => opt.MapFrom(src => src.Department.Name))
                .ForMember(dest => dest.ManagerName,
                    opt => opt.MapFrom(src => src.Manager != null ? src.Manager.FullName : null));

            CreateMap<Employee, EmployeeListDto>()
                .ForMember(dest => dest.DepartmentName,
                    opt => opt.MapFrom(src => src.Department.Name));

            CreateMap<CreateEmployeeDto, Employee>()
                .ForMember(dest => dest.EmployeeCode,
                    opt => opt.Ignore())
                .ForMember(dest => dest.Status,
                    opt => opt.MapFrom(src => EmployeeStatus.Active))
                .ForMember(dest => dest.CreatedAt,
                    opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt,
                    opt => opt.Ignore());

            CreateMap<UpdateEmployeeDto, Employee>()
                .ForMember(dest => dest.EmployeeCode,
                    opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt,
                    opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt,
                    opt => opt.Ignore())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // Department mappings
            // Department mappings
            CreateMap<Department, DepartmentDto>()
                .ForMember(dest => dest.ManagerName,
                    opt => opt.MapFrom(src => src.Manager != null ? src.Manager.FullName : null))
                .ForMember(dest => dest.EmployeeCount,
                    opt => opt.MapFrom(src => src.Employees != null ? src.Employees.Count : 0));

            CreateMap<Department, DepartmentListDto>()
                .ForMember(dest => dest.ManagerName,
                    opt => opt.MapFrom(src => src.Manager != null ? src.Manager.FullName : null))
                .ForMember(dest => dest.EmployeeCount,
                    opt => opt.MapFrom(src => src.Employees != null ? src.Employees.Count : 0));

            CreateMap<Department, DepartmentDetailDto>()
                .IncludeBase<Department, DepartmentDto>()
                .ForMember(dest => dest.Employees,
                    opt => opt.MapFrom(src => src.Employees));

            CreateMap<CreateDepartmentDto, Department>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
                .ForMember(dest => dest.Manager, opt => opt.Ignore())
                .ForMember(dest => dest.Employees, opt => opt.Ignore());

            CreateMap<UpdateDepartmentDto, Department>()
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
                .ForMember(dest => dest.Manager, opt => opt.Ignore())
                .ForMember(dest => dest.Employees, opt => opt.Ignore())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // Leave mappings
            CreateMap<LeaveRequest, LeaveRequestDto>()
                .ForMember(dest => dest.EmployeeName,
                    opt => opt.MapFrom(src => src.Employee.FullName))
                .ForMember(dest => dest.EmployeeCode,
                    opt => opt.MapFrom(src => src.Employee.EmployeeCode))
                .ForMember(dest => dest.ApprovedByName,
                    opt => opt.MapFrom(src => src.ApprovedBy != null ? src.ApprovedBy.FullName : null));

            CreateMap<CreateLeaveRequestDto, LeaveRequest>()
                .ForMember(dest => dest.Status,
                    opt => opt.MapFrom(src => LeaveStatus.Pending))
                .ForMember(dest => dest.TotalDays,
                    opt => opt.Ignore());

            // Attendance mappings
            CreateMap<Core.Entities.Attendance, AttendanceDto>()
                .ForMember(dest => dest.EmployeeName,
                    opt => opt.MapFrom(src => src.Employee != null ? src.Employee.FullName : string.Empty))
                .ForMember(dest => dest.EmployeeCode,
                    opt => opt.MapFrom(src => src.Employee != null ? src.Employee.EmployeeCode : string.Empty));

            CreateMap<CreateAttendanceDto, Core.Entities.Attendance>()
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.TotalHours, opt => opt.Ignore())
                .ForMember(dest => dest.OvertimeHours, opt => opt.Ignore())
                .ForMember(dest => dest.LateMinutes, opt => opt.Ignore());

            // Performance review mappings
            CreateMap<Core.Entities.PerformanceReview, PerformanceReviewDto>()
                .ForMember(dest => dest.EmployeeName,
                    opt => opt.MapFrom(src => src.Employee != null ? src.Employee.FullName : string.Empty))
                .ForMember(dest => dest.EmployeeCode,
                    opt => opt.MapFrom(src => src.Employee != null ? src.Employee.EmployeeCode : string.Empty))
                .ForMember(dest => dest.Department,
                    opt => opt.MapFrom(src => src.Employee != null && src.Employee.Department != null
                        ? src.Employee.Department.Name : string.Empty))
                .ForMember(dest => dest.ReviewerName,
                    opt => opt.MapFrom(src => src.Reviewer != null ? src.Reviewer.FullName : string.Empty));

            CreateMap<CreatePerformanceReviewDto, Core.Entities.PerformanceReview>()
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

            // Payroll mappings
            CreateMap<Core.Entities.Payroll, PayrollDto>()
                .ForMember(dest => dest.EmployeeName,
                    opt => opt.MapFrom(src => src.Employee != null ? src.Employee.FullName : string.Empty))
                .ForMember(dest => dest.EmployeeCode,
                    opt => opt.MapFrom(src => src.Employee != null ? src.Employee.EmployeeCode : string.Empty))
                .ForMember(dest => dest.Department,
                    opt => opt.MapFrom(src => src.Employee != null && src.Employee.Department != null
                        ? src.Employee.Department.Name : string.Empty))
                .ForMember(dest => dest.JobTitle,
                    opt => opt.MapFrom(src => src.Employee != null ? src.Employee.JobTitle : string.Empty))
                .ForMember(dest => dest.ProcessedByName,
                    opt => opt.MapFrom(src => src.ProcessedBy != null ? src.ProcessedBy.FullName : null))
                .ForMember(dest => dest.ApprovedByName,
                    opt => opt.MapFrom(src => src.ApprovedBy != null ? src.ApprovedBy.FullName : null));

            // CQRS command → DTO mappings (commands map to existing DTOs for validation reuse)
            CreateMap<CreateEmployeeCommand, CreateEmployeeDto>();
            CreateMap<UpdateEmployeeCommand, UpdateEmployeeDto>();
        }
    }
}
