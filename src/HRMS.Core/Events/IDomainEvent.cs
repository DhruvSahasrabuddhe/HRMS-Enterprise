namespace HRMS.Core.Events
{
    /// <summary>
    /// Marker interface for all domain events. Domain events represent
    /// something that has happened in the domain and are used to
    /// decouple side effects from the core business logic.
    /// </summary>
    public interface IDomainEvent
    {
        /// <summary>The UTC timestamp when this event occurred.</summary>
        DateTime OccurredOn { get; }

        /// <summary>Unique identifier for this event instance.</summary>
        Guid EventId { get; }
    }
}
