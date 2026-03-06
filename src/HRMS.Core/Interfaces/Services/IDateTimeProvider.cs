namespace HRMS.Core.Interfaces.Services
{
    /// <summary>
    /// Abstraction for date and time operations to improve testability.
    /// </summary>
    public interface IDateTimeProvider
    {
        /// <summary>
        /// Gets the current date and time.
        /// </summary>
        DateTime Now { get; }

        /// <summary>
        /// Gets the current date with time set to midnight.
        /// </summary>
        DateTime Today { get; }

        /// <summary>
        /// Gets the current UTC date and time.
        /// </summary>
        DateTime UtcNow { get; }
    }
}
