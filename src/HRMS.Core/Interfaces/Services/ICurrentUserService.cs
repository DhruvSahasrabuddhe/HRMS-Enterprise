namespace HRMS.Core.Interfaces.Services
{
    /// <summary>
    /// Abstraction for resolving the currently authenticated user.
    /// This decouples the domain/infrastructure layers from HTTP-specific dependencies.
    /// </summary>
    public interface ICurrentUserService
    {
        /// <summary>The username (or null when unauthenticated / running as a background job).</summary>
        string? UserName { get; }

        /// <summary>The unique user identifier (e.g. ASP.NET Identity user ID), or null.</summary>
        string? UserId { get; }

        /// <summary>True when there is an authenticated user in the current context.</summary>
        bool IsAuthenticated { get; }
    }
}
