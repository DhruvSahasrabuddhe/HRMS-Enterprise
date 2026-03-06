namespace HRMS.Core.Interfaces.Services
{
    /// <summary>
    /// Service for generating unique employee codes.
    /// </summary>
    public interface IEmployeeCodeGenerator
    {
        /// <summary>
        /// Generates a unique employee code based on the current year and sequence.
        /// </summary>
        /// <returns>A unique employee code in the format EMP{YYYY}{SEQUENCE}.</returns>
        Task<string> GenerateEmployeeCodeAsync();
    }
}
