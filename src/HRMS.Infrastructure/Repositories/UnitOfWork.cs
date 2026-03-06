using HRMS.Core.Interfaces.Repositories;
using HRMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace HRMS.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private IDbContextTransaction? _transaction;
        private bool _disposed = false;

        private IEmployeeRepository? _employeeRepository;
        private IDepartmentRepository? _departmentRepository;
        private ILeaveRepository? _leaveRepository;
        private IAttendanceRepository? _attendanceRepository;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
        }

        public IEmployeeRepository Employees =>
            _employeeRepository ??= new EmployeeRepository(_context);

        public IDepartmentRepository Departments =>
            _departmentRepository ??= new DepartmentRepository(_context);

        public ILeaveRepository Leaves =>
            _leaveRepository ??= new LeaveRepository(_context);

        public IAttendanceRepository Attendances =>
            _attendanceRepository ??= new AttendanceRepository(_context);

        public async Task<int> CompleteAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            try
            {
                await SaveChangesAsync();
                if (_transaction != null)
                {
                    await _transaction.CommitAsync();
                }
            }
            catch
            {
                await RollbackTransactionAsync();
                throw;
            }
            finally
            {
                if (_transaction != null)
                {
                    await _transaction.DisposeAsync();
                    _transaction = null;
                }
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _context.Dispose();
                if (_transaction != null)
                {
                    _transaction.Dispose();
                    _transaction = null;
                }
            }
            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}