namespace HRMS.Core.Exceptions
{
    /// <summary>
    /// Represents a business-rule violation that should be surfaced to the caller
    /// as a bad-request (HTTP 400) rather than an internal server error.
    /// </summary>
    public class BusinessException : HrmsException
    {
        /// <summary>
        /// Initialises a new <see cref="BusinessException"/> with a human-readable
        /// <paramref name="message"/> and a machine-readable <paramref name="errorCode"/>.
        /// </summary>
        public BusinessException(string message, string errorCode = "BUSINESS_RULE_VIOLATION")
            : base(message, errorCode) { }

        /// <summary>
        /// Initialises a new <see cref="BusinessException"/> while preserving the
        /// original <paramref name="innerException"/> for diagnostic purposes.
        /// </summary>
        public BusinessException(string message, string errorCode, Exception innerException)
            : base(message, errorCode, innerException) { }
    }
}
