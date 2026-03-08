using HRMS.Shared.Common;
using HRMS.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Web.Controllers.Api
{
    /// <summary>
    /// Base class for all HRMS REST API controllers.  Provides helpers for adding
    /// pagination headers, cache-control headers, and rate-limit response headers
    /// so every derived controller benefits from these cross-cutting concerns without
    /// repeating boilerplate.
    /// </summary>
    /// <remarks>
    /// <para><strong>CSRF protection</strong>: REST API endpoints do not use antiforgery
    /// tokens.  Protection against cross-site request forgery is provided by the
    /// ASP.NET Core Identity cookie's <c>SameSite=Lax</c> attribute (the default),
    /// which prevents the browser from sending the session cookie on cross-origin
    /// requests initiated by third-party pages.  Consumers that are not browsers
    /// (mobile clients, server-to-server integrations) must supply the cookie
    /// explicitly and are therefore outside the CSRF threat model.
    /// The <see cref="IgnoreAntiforgeryTokenAttribute"/> is applied here to make
    /// this security decision explicit and avoid false-positive analyzer warnings.</para>
    /// </remarks>
    [Authorize]
    [ApiController]
    [IgnoreAntiforgeryToken]
    [Route(HrmsConstants.Api.RoutePrefix + "/[controller]")]
    [Produces("application/json")]
    public abstract class ApiControllerBase : ControllerBase
    {
        // ── Pagination ────────────────────────────────────────────────────────────

        /// <summary>
        /// Writes X-Total-Count, X-Total-Pages, X-Current-Page and X-Page-Size headers
        /// from a <see cref="PagedResult{T}"/> so consumers can navigate pages without
        /// having to parse the response body.
        /// </summary>
        protected void AddPaginationHeaders<T>(PagedResult<T> pagedResult)
        {
            Response.Headers[HrmsConstants.Api.TotalCountHeader] = pagedResult.TotalCount.ToString();
            Response.Headers[HrmsConstants.Api.TotalPagesHeader] = pagedResult.TotalPages.ToString();
            Response.Headers[HrmsConstants.Api.CurrentPageHeader] = pagedResult.PageNumber.ToString();
            Response.Headers[HrmsConstants.Api.PageSizeHeader] = pagedResult.PageSize.ToString();

            // Expose custom headers to browser JS (CORS pre-flight)
            Response.Headers.Append("Access-Control-Expose-Headers",
                string.Join(", ",
                    HrmsConstants.Api.TotalCountHeader,
                    HrmsConstants.Api.TotalPagesHeader,
                    HrmsConstants.Api.CurrentPageHeader,
                    HrmsConstants.Api.PageSizeHeader));
        }

        // ── Cache control ─────────────────────────────────────────────────────────

        /// <summary>
        /// Adds a <c>Cache-Control: no-store</c> header to prevent caching of sensitive
        /// or mutable responses (e.g. employee data with PII).
        /// </summary>
        protected void SetNoCache()
        {
            Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
            Response.Headers.Pragma = "no-cache";
        }

        /// <summary>
        /// Adds a <c>Cache-Control: public, max-age=<paramref name="seconds"/></c> header
        /// for read-only reference data that rarely changes (e.g. department list).
        /// </summary>
        protected void SetPublicCache(int seconds)
        {
            Response.Headers.CacheControl = $"public, max-age={seconds}";
        }

        /// <summary>
        /// Adds a private <c>Cache-Control</c> header with the given max-age.
        /// Suitable for responses that may be cached by the client but not shared
        /// proxies (e.g. per-user data).
        /// </summary>
        protected void SetPrivateCache(int seconds)
        {
            Response.Headers.CacheControl = $"private, max-age={seconds}";
        }

        // ── Rate-limit headers ────────────────────────────────────────────────────

        /// <summary>
        /// Writes informational rate-limit headers so API consumers can self-throttle
        /// before hitting a hard limit.  Values are drawn from
        /// <see cref="HrmsConstants.Security"/> and are approximate – they convey
        /// the quota window rather than acting as a strict enforcement mechanism
        /// (enforcement is handled at the infrastructure / gateway layer).
        /// </summary>
        protected void AddRateLimitHeaders()
        {
            Response.Headers[HrmsConstants.Api.RateLimitLimitHeader] =
                HrmsConstants.Security.GeneralRateLimitRequests.ToString();

            // Remaining is not tracked per-request here; expose the full quota so
            // consumers know the ceiling.  A real implementation would decrement
            // a counter stored in distributed cache keyed by client IP or JWT sub.
            Response.Headers[HrmsConstants.Api.RateLimitRemainingHeader] =
                HrmsConstants.Security.GeneralRateLimitRequests.ToString();

            // Reset = start of the next 60-second window.
            var resetEpoch = DateTimeOffset.UtcNow
                .AddSeconds(HrmsConstants.Security.GeneralRateLimitPeriodSeconds)
                .ToUnixTimeSeconds();
            Response.Headers[HrmsConstants.Api.RateLimitResetHeader] = resetEpoch.ToString();
        }
    }
}
