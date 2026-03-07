using HRMS.Shared.Constants;
using HRMS.Web.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace HRMS.UnitTests.Middleware
{
    public class CorrelationIdMiddlewareTests
    {
        private readonly Mock<ILogger<CorrelationIdMiddleware>> _loggerMock
            = new Mock<ILogger<CorrelationIdMiddleware>>();

        private CorrelationIdMiddleware CreateMiddleware(RequestDelegate next)
            => new CorrelationIdMiddleware(next);

        [Fact]
        public async Task InvokeAsync_WhenNoHeaderPresent_GeneratesNewCorrelationId()
        {
            // Arrange
            string? capturedCorrelationId = null;
            var middleware = CreateMiddleware(ctx =>
            {
                capturedCorrelationId = ctx.Items[HrmsConstants.Logging.CorrelationIdItemKey]?.ToString();
                return Task.CompletedTask;
            });

            var context = new DefaultHttpContext();

            // Act
            await middleware.InvokeAsync(context, _loggerMock.Object);

            // Assert
            Assert.NotNull(capturedCorrelationId);
            Assert.True(Guid.TryParse(capturedCorrelationId, out _),
                "Generated correlation ID should be a valid GUID.");
        }

        [Fact]
        public async Task InvokeAsync_WhenHeaderPresent_PropagatesExistingId()
        {
            // Arrange
            var existingId = "test-correlation-123";
            string? capturedCorrelationId = null;
            var middleware = CreateMiddleware(ctx =>
            {
                capturedCorrelationId = ctx.Items[HrmsConstants.Logging.CorrelationIdItemKey]?.ToString();
                return Task.CompletedTask;
            });

            var context = new DefaultHttpContext();
            context.Request.Headers[HrmsConstants.Logging.CorrelationIdHeader] = existingId;

            // Act
            await middleware.InvokeAsync(context, _loggerMock.Object);

            // Assert
            Assert.Equal(existingId, capturedCorrelationId);
        }

        [Fact]
        public async Task InvokeAsync_SetsCorrelationIdOnResponseHeader()
        {
            // Arrange
            var existingId = "response-header-test";
            var middleware = CreateMiddleware(_ => Task.CompletedTask);

            var context = new DefaultHttpContext();
            context.Request.Headers[HrmsConstants.Logging.CorrelationIdHeader] = existingId;

            // Act
            await middleware.InvokeAsync(context, _loggerMock.Object);

            // Assert
            Assert.Equal(existingId,
                context.Response.Headers[HrmsConstants.Logging.CorrelationIdHeader].ToString());
        }

        [Fact]
        public async Task InvokeAsync_StoresCorrelationIdInHttpContextItems()
        {
            // Arrange
            var middleware = CreateMiddleware(ctx => Task.CompletedTask);
            var context = new DefaultHttpContext();

            // Act
            await middleware.InvokeAsync(context, _loggerMock.Object);

            // Assert
            Assert.True(context.Items.ContainsKey(HrmsConstants.Logging.CorrelationIdItemKey));
            Assert.NotNull(context.Items[HrmsConstants.Logging.CorrelationIdItemKey]);
        }
    }
}
