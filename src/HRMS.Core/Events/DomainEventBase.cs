namespace HRMS.Core.Events
{
    /// <summary>
    /// Convenience base class for domain events providing standard properties.
    /// </summary>
    public abstract class DomainEventBase : IDomainEvent
    {
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
        public Guid EventId { get; } = Guid.NewGuid();
    }
}
