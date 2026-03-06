using AutoMapper;
using HRMS.Core.Entities;
using HRMS.Core.Enums;
using HRMS.Services.Departments.Dtos;
using HRMS.Services.Employees.Commands;
using HRMS.Services.Employees.Dtos;
using HRMS.Services.Leave.Dtos;

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

            // CQRS command → DTO mappings (commands map to existing DTOs for validation reuse)
            CreateMap<CreateEmployeeCommand, CreateEmployeeDto>();
            CreateMap<UpdateEmployeeCommand, UpdateEmployeeDto>();
        }
    }
}