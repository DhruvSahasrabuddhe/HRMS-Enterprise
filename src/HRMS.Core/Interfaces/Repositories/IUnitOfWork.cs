namespace HRMS.Core.Interfaces.Repositories
{
    public interface IUnitOfWork : IDisposable
    {
        IEmployeeRepository Employees { get; }
        IDepartmentRepository Departments { get; }
        ILeaveRepository Leaves { get; }
        IAttendanceRepository Attendances { get; }
        IPerformanceReviewRepository PerformanceReviews { get; }
        IPayrollRepository Payrolls { get; }

        Task<int> CompleteAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
        Task<int> SaveChangesAsync();
    }
}