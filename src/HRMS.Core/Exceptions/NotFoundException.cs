namespace HRMS.Core.Exceptions
{
    /// <summary>
    /// Thrown when a requested resource cannot be located in the data store.
    /// Maps to HTTP 404 Not Found.
    /// </summary>
    public class NotFoundException : HrmsException
    {
        /// <summary>
        /// Initialises a new <see cref="NotFoundException"/> for a resource identified
        /// by type name and identifier.
        /// </summary>
        /// <param name="resourceName">The display name of the resource type (e.g. "Employee").</param>
        /// <param name="id">The identifier that was used in the lookup.</param>
        public NotFoundException(string resourceName, object id)
            : base($"{resourceName} with identifier '{id}' was not found.", "NOT_FOUND") { }

        /// <summary>
        /// Initialises a new <see cref="NotFoundException"/> with a custom message.
        /// </summary>
        public NotFoundException(string message)
            : base(message, "NOT_FOUND") { }
    }
}
