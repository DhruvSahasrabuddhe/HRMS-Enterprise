namespace HRMS.Core.Events
{
    /// <summary>
    /// Implemented by entities that can raise domain events.
    /// The events are collected in memory and dispatched after the
    /// Unit of Work commits the transaction.
    /// </summary>
    public interface IHasDomainEvents
    {
        IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
        void AddDomainEvent(IDomainEvent domainEvent);
        void ClearDomainEvents();
    }
}
