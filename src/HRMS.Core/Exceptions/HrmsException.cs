namespace HRMS.Core.Exceptions
{
    /// <summary>
    /// Base exception for all HRMS application exceptions.
    /// Provides a structured error code alongside the standard exception message.
    /// </summary>
    public abstract class HrmsException : Exception
    {
        /// <summary>
        /// A short, machine-readable code identifying the error category
        /// (e.g. "EMPLOYEE_NOT_FOUND", "DUPLICATE_EMAIL").
        /// </summary>
        public string ErrorCode { get; }

        protected HrmsException(string message, string errorCode)
            : base(message)
        {
            ErrorCode = errorCode;
        }

        protected HrmsException(string message, string errorCode, Exception innerException)
            : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }
}
