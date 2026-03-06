using HRMS.Core.Interfaces.Services;
using Microsoft.AspNetCore.Http;

namespace HRMS.Infrastructure.Services
{
    /// <summary>
    /// Resolves the currently authenticated user from the HTTP context.
    /// Falls back to "System" when there is no active HTTP context (e.g. background jobs).
    /// </summary>
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string? UserName =>
            _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

        public string? UserId =>
            _httpContextAccessor.HttpContext?.User?.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        public bool IsAuthenticated =>
            _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
    }
}
