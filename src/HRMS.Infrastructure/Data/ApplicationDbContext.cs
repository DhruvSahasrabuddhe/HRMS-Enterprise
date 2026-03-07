using HRMS.Core.Entities;
using HRMS.Core.Entities.Base;
using HRMS.Core.Interfaces.Services;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace HRMS.Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        private readonly ICurrentUserService? _currentUserService;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            ICurrentUserService currentUserService)
            : base(options)
        {
            _currentUserService = currentUserService;
        }

        // DbSets
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<LeaveRequest> LeaveRequests { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<PerformanceReview> PerformanceReviews { get; set; }
        public DbSet<Payroll> Payrolls { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Apply all configurations from this assembly
            builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

            // Global query filters
            builder.Entity<Employee>().HasQueryFilter(e => !e.IsDeleted);
            builder.Entity<Department>().HasQueryFilter(d => !d.IsDeleted);
            builder.Entity<LeaveRequest>().HasQueryFilter(l => !l.IsDeleted);
            builder.Entity<Attendance>().HasQueryFilter(a => !a.IsDeleted);
            builder.Entity<PerformanceReview>().HasQueryFilter(r => !r.IsDeleted);
            builder.Entity<Payroll>().HasQueryFilter(p => !p.IsDeleted);

            // Configure relationships
            ConfigureRelationships(builder);

            // Configure indexes
            ConfigureIndexes(builder);

            // Seed initial data
            SeedData(builder);
        }

        private void ConfigureRelationships(ModelBuilder builder)
        {
            // Employee - Department relationship
            builder.Entity<Employee>()
                .HasOne(e => e.Department)
                .WithMany(d => d.Employees)
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Employee - Manager relationship (self-referencing)
            builder.Entity<Employee>()
                .HasOne(e => e.Manager)
                .WithMany(e => e.Subordinates)
                .HasForeignKey(e => e.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);

            // LeaveRequest - Employee relationship
            builder.Entity<LeaveRequest>()
                .HasOne(l => l.Employee)
                .WithMany(e => e.LeaveRequests)
                .HasForeignKey(l => l.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            // LeaveRequest - ApprovedBy relationship
            builder.Entity<LeaveRequest>()
                .HasOne(l => l.ApprovedBy)
                .WithMany()
                .HasForeignKey(l => l.ApprovedById)
                .OnDelete(DeleteBehavior.Restrict);

            // Attendance - Employee relationship
            builder.Entity<Attendance>()
                .HasOne(a => a.Employee)
                .WithMany(e => e.Attendances)
                .HasForeignKey(a => a.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            // PerformanceReview - Employee relationship
            builder.Entity<PerformanceReview>()
                .HasOne(r => r.Employee)
                .WithMany()
                .HasForeignKey(r => r.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            // PerformanceReview - Reviewer relationship
            builder.Entity<PerformanceReview>()
                .HasOne(r => r.Reviewer)
                .WithMany()
                .HasForeignKey(r => r.ReviewerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Payroll - Employee relationship
            builder.Entity<Payroll>()
                .HasOne(p => p.Employee)
                .WithMany()
                .HasForeignKey(p => p.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Payroll - ProcessedBy relationship
            builder.Entity<Payroll>()
                .HasOne(p => p.ProcessedBy)
                .WithMany()
                .HasForeignKey(p => p.ProcessedById)
                .OnDelete(DeleteBehavior.Restrict);

            // Payroll - ApprovedBy relationship
            builder.Entity<Payroll>()
                .HasOne(p => p.ApprovedBy)
                .WithMany()
                .HasForeignKey(p => p.ApprovedById)
                .OnDelete(DeleteBehavior.Restrict);
        }

        private void ConfigureIndexes(ModelBuilder builder)
        {
            // Employee indexes
            builder.Entity<Employee>()
                .HasIndex(e => e.EmployeeCode)
                .IsUnique();

            builder.Entity<Employee>()
                .HasIndex(e => e.Email)
                .IsUnique();

            builder.Entity<Employee>()
                .HasIndex(e => new { e.FirstName, e.LastName });

            builder.Entity<Employee>()
                .HasIndex(e => e.DepartmentId);

            builder.Entity<Employee>()
                .HasIndex(e => e.ManagerId);

            builder.Entity<Employee>()
                .HasIndex(e => e.Status);

            // Composite index: supports dashboard queries that filter on both Status and DepartmentId.
            builder.Entity<Employee>()
                .HasIndex(e => new { e.Status, e.DepartmentId });

            // HireDate index: supports date-range hire reports.
            builder.Entity<Employee>()
                .HasIndex(e => e.HireDate);

            // Department indexes
            builder.Entity<Department>()
                .HasIndex(d => d.Code)
                .IsUnique();

            builder.Entity<Department>()
                .HasIndex(d => d.Name)
                .IsUnique();

            builder.Entity<Department>()
                .HasIndex(d => d.ManagerId);

            // LeaveRequest indexes
            builder.Entity<LeaveRequest>()
                .HasIndex(l => l.EmployeeId);

            builder.Entity<LeaveRequest>()
                .HasIndex(l => l.Status);

            // Composite index: supports leave-balance and pending-approval queries.
            builder.Entity<LeaveRequest>()
                .HasIndex(l => new { l.EmployeeId, l.Status });

            builder.Entity<LeaveRequest>()
                .HasIndex(l => new { l.StartDate, l.EndDate });

            // Attendance indexes
            builder.Entity<Attendance>()
                .HasIndex(a => a.EmployeeId);

            builder.Entity<Attendance>()
                .HasIndex(a => a.Date);

            builder.Entity<Attendance>()
                .HasIndex(a => new { a.EmployeeId, a.Date })
                .IsUnique();

            // AuditLog indexes: support entity-history and user-activity queries.
            builder.Entity<AuditLog>()
                .HasIndex(a => new { a.Entity, a.EntityId });

            builder.Entity<AuditLog>()
                .HasIndex(a => new { a.UserId, a.Timestamp });

            // PerformanceReview indexes
            builder.Entity<PerformanceReview>()
                .HasIndex(r => r.EmployeeId);

            builder.Entity<PerformanceReview>()
                .HasIndex(r => r.ReviewerId);

            builder.Entity<PerformanceReview>()
                .HasIndex(r => r.Status);

            builder.Entity<PerformanceReview>()
                .HasIndex(r => new { r.EmployeeId, r.ReviewYear, r.CycleType });

            builder.Entity<PerformanceReview>()
                .HasIndex(r => r.DueDate);

            // Payroll indexes
            builder.Entity<Payroll>()
                .HasIndex(p => p.EmployeeId);

            builder.Entity<Payroll>()
                .HasIndex(p => new { p.Year, p.Month });

            builder.Entity<Payroll>()
                .HasIndex(p => new { p.EmployeeId, p.Year, p.Month })
                .IsUnique();

            builder.Entity<Payroll>()
                .HasIndex(p => p.Status);
        }

        private void SeedData(ModelBuilder builder)
        {
            // Seed default admin user (will be handled by DbInitializer)
            // This is just for reference
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entries = ChangeTracker.Entries<BaseEntity>();
            var now = DateTime.UtcNow;
            var currentUser = GetCurrentUser(); // You'll need to implement this

            foreach (var entry in entries)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedAt = now;
                        entry.Entity.CreatedBy = currentUser;
                        break;
                    case EntityState.Modified:
                        entry.Entity.UpdatedAt = now;
                        entry.Entity.UpdatedBy = currentUser;
                        break;
                    case EntityState.Deleted:
                        entry.State = EntityState.Modified;
                        entry.Entity.IsDeleted = true;
                        entry.Entity.UpdatedAt = now;
                        entry.Entity.UpdatedBy = currentUser;
                        break;
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }

        private string? GetCurrentUser()
        {
            return _currentUserService?.UserName ?? "System";
        }
    }
}