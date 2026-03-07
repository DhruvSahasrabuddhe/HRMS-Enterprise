using System.Net;
using System.Text.Json;
using HRMS.Core.Exceptions;
using HRMS.Web.Middleware;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace HRMS.UnitTests.Middleware
{
    public class GlobalExceptionMiddlewareTests
    {
        private readonly Mock<ILogger<GlobalExceptionMiddleware>> _loggerMock
            = new Mock<ILogger<GlobalExceptionMiddleware>>();

        private readonly Mock<IWebHostEnvironment> _envMock = new Mock<IWebHostEnvironment>();

        private GlobalExceptionMiddleware CreateMiddleware(RequestDelegate next,
            bool isDevelopment = false)
        {
            _envMock.Setup(e => e.EnvironmentName)
                    .Returns(isDevelopment ? "Development" : "Production");
            return new GlobalExceptionMiddleware(next, _loggerMock.Object, _envMock.Object);
        }

        private static DefaultHttpContext CreateContext()
        {
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            return context;
        }

        private static async Task<(int StatusCode, JsonDocument Body)> ExecuteAsync(
            GlobalExceptionMiddleware middleware, HttpContext context)
        {
            await middleware.InvokeAsync(context);
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var json = await new StreamReader(context.Response.Body).ReadToEndAsync();
            var doc = string.IsNullOrWhiteSpace(json)
                ? JsonDocument.Parse("{}")
                : JsonDocument.Parse(json);
            return (context.Response.StatusCode, doc);
        }

        [Fact]
        public async Task InvokeAsync_WhenNoException_DoesNotModifyResponse()
        {
            var middleware = CreateMiddleware(_ => Task.CompletedTask);
            var context = CreateContext();

            await middleware.InvokeAsync(context);

            // Default status code is 200 and body is empty
            Assert.Equal(200, context.Response.StatusCode);
        }

        [Fact]
        public async Task InvokeAsync_WhenNotFoundException_Returns404()
        {
            var middleware = CreateMiddleware(_ => throw new NotFoundException("Employee", 99));
            var context = CreateContext();

            var (status, body) = await ExecuteAsync(middleware, context);

            Assert.Equal((int)HttpStatusCode.NotFound, status);
            Assert.Equal("NOT_FOUND", body.RootElement.GetProperty("errorCode").GetString());
        }

        [Fact]
        public async Task InvokeAsync_WhenBusinessException_Returns400()
        {
            var middleware = CreateMiddleware(
                _ => throw new BusinessException("Cannot delete active employee.", "ACTIVE_EMPLOYEE"));
            var context = CreateContext();

            var (status, body) = await ExecuteAsync(middleware, context);

            Assert.Equal((int)HttpStatusCode.BadRequest, status);
            Assert.Equal("ACTIVE_EMPLOYEE", body.RootElement.GetProperty("errorCode").GetString());
        }

        [Fact]
        public async Task InvokeAsync_WhenUnexpectedException_Returns500()
        {
            var middleware = CreateMiddleware(_ => throw new InvalidCastException("boom"));
            var context = CreateContext();

            var (status, body) = await ExecuteAsync(middleware, context);

            Assert.Equal((int)HttpStatusCode.InternalServerError, status);
            Assert.Equal("INTERNAL_ERROR", body.RootElement.GetProperty("errorCode").GetString());
        }

        [Fact]
        public async Task InvokeAsync_WhenKeyNotFoundException_Returns404()
        {
            var middleware = CreateMiddleware(_ => throw new KeyNotFoundException("Key not found."));
            var context = CreateContext();

            var (status, body) = await ExecuteAsync(middleware, context);

            Assert.Equal((int)HttpStatusCode.NotFound, status);
            Assert.Equal("NOT_FOUND", body.RootElement.GetProperty("errorCode").GetString());
        }

        [Fact]
        public async Task InvokeAsync_InDevelopment_IncludesStackTrace()
        {
            var middleware = CreateMiddleware(
                _ => throw new BusinessException("biz error"), isDevelopment: true);
            var context = CreateContext();

            var (_, body) = await ExecuteAsync(middleware, context);

            Assert.True(body.RootElement.TryGetProperty("stackTrace", out _),
                "Development mode should include stackTrace in the response.");
        }

        [Fact]
        public async Task InvokeAsync_InProduction_ExcludesStackTrace()
        {
            var middleware = CreateMiddleware(
                _ => throw new BusinessException("biz error"), isDevelopment: false);
            var context = CreateContext();

            var (_, body) = await ExecuteAsync(middleware, context);

            Assert.False(body.RootElement.TryGetProperty("stackTrace", out _),
                "Production mode should NOT include stackTrace in the response.");
        }

        [Fact]
        public async Task InvokeAsync_ResponseIncludesCorrelationId()
        {
            var correlationId = "unit-test-correlation-xyz";
            var middleware = CreateMiddleware(_ => throw new BusinessException("err"));
            var context = CreateContext();
            context.Items[HRMS.Shared.Constants.HrmsConstants.Logging.CorrelationIdItemKey] = correlationId;

            var (_, body) = await ExecuteAsync(middleware, context);

            Assert.Equal(correlationId, body.RootElement.GetProperty("correlationId").GetString());
        }
    }
}
