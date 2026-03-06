using HRMS.Core.Events;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.Core.Entities.Base
{
    public abstract class BaseEntity : IHasDomainEvents
    {
        [Key]
        public int Id { get; set; }

        public DateTime CreatedAt { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public string? UpdatedBy { get; set; }

        public bool IsDeleted { get; set; }

        [Timestamp]
        public byte[]? RowVersion { get; set; }

        // Domain events – not mapped to the database
        [NotMapped]
        private readonly List<IDomainEvent> _domainEvents = new();

        [NotMapped]
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        public void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

        public void ClearDomainEvents() => _domainEvents.Clear();
    }
}