using HRMS.Core.Interfaces.Services;

namespace HRMS.Infrastructure.Services
{
    /// <summary>
    /// Default implementation of IDateTimeProvider using system DateTime.
    /// </summary>
    public class DateTimeProvider : IDateTimeProvider
    {
        /// <inheritdoc />
        public DateTime Now => DateTime.Now;

        /// <inheritdoc />
        public DateTime Today => DateTime.Today;

        /// <inheritdoc />
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
