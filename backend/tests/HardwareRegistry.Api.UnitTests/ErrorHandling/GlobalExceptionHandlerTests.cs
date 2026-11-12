using System.Text.Json;
using HardwareRegistry.Api.ErrorHandling;
using HardwareRegistry.Application.Common.Exceptions;
using HardwareRegistry.Domain.Devices;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace HardwareRegistry.Api.UnitTests.ErrorHandling
{
    public sealed class GlobalExceptionHandlerTests
    {
        private readonly FakeLogger<GlobalExceptionHandler> _logger = new();

        [Fact]
        public async Task TryHandleAsync_PersistenceConflict_WritesConflictProblem()
        {
            var (context, body) = GlobalExceptionHandlerTests.CreateContext();
            var exception = new PersistenceConflictException(typeof(DeviceEntity), "InventoryNumber", "PC1");

            var isHandled = await new GlobalExceptionHandler(this._logger)
                .TryHandleAsync(context, exception, TestContext.Current.CancellationToken);

            var problem = await GlobalExceptionHandlerTests.ReadProblemAsync(body);
            Assert.True(isHandled);
            Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
            Assert.Equal(409, problem.GetProperty("status").GetInt32());
            Assert.Equal(exception.Message, problem.GetProperty("detail").GetString());
            Assert.Equal("/api/devices", problem.GetProperty("instance").GetString());
            Assert.True(problem.TryGetProperty("traceId", out _));
        }

        [Fact]
        public async Task TryHandleAsync_UnexpectedException_WritesGenericServerErrorWithoutInternals()
        {
            var (context, body) = GlobalExceptionHandlerTests.CreateContext();

            var isHandled = await new GlobalExceptionHandler(this._logger).TryHandleAsync(
                context,
                new InvalidOperationException("Connection string password=secret"),
                TestContext.Current.CancellationToken
            );

            var problem = await GlobalExceptionHandlerTests.ReadProblemAsync(body);
            Assert.True(isHandled);
            Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
            Assert.Equal("An unexpected error occurred.", problem.GetProperty("detail").GetString());
            Assert.DoesNotContain("secret", problem.GetRawText());
        }

        [Fact]
        public async Task TryHandleAsync_AnyException_LogsErrorWithException()
        {
            var (context, _) = GlobalExceptionHandlerTests.CreateContext();
            var exception = new InvalidOperationException();

            await new GlobalExceptionHandler(this._logger)
                .TryHandleAsync(context, exception, TestContext.Current.CancellationToken);

            var record = Assert.Single(this._logger.Collector.GetSnapshot());
            Assert.Equal(LogLevel.Error, record.Level);
            Assert.Same(exception, record.Exception);
        }

        private static (DefaultHttpContext Context, MemoryStream Body) CreateContext()
        {
            var body = new MemoryStream();
            var context = new DefaultHttpContext
            {
                Request = { Method = "POST", Path = "/api/devices" },
                Response = { Body = body }
            };

            return (context, body);
        }

        private static async Task<JsonElement> ReadProblemAsync(MemoryStream body)
        {
            body.Position = 0;
            using var document = await JsonDocument.ParseAsync(body);
            return document.RootElement.Clone();
        }
    }
}
