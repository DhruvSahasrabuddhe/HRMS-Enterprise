using HRMS.Core.Entities;
using System.Linq.Expressions;

namespace HRMS.Core.Specifications
{
    public class DepartmentWithEmployeesSpecification : BaseSpecification<Department>
    {
        public DepartmentWithEmployeesSpecification(int id)
            : base(d => d.Id == id)
        {
            AddInclude(d => d.Employees!);
            AddInclude(d => d.Manager!);
        }

        public DepartmentWithEmployeesSpecification(string name)
            : base(d => d.Name != null && d.Name.Contains(name))
        {
            AddInclude(d => d.Employees!);
            AddInclude(d => d.Manager!);
        }
    }

    public class DepartmentsWithManagersSpecification : BaseSpecification<Department>
    {
        public DepartmentsWithManagersSpecification()
        {
            AddInclude(d => d.Manager!);
            AddInclude(d => d.Employees!);
        }

        public DepartmentsWithManagersSpecification(bool hasManager)
            : base(d => hasManager ? d.Manager != null : d.Manager == null)
        {
            AddInclude(d => d.Manager!);
            AddInclude(d => d.Employees!);
        }
    }
}