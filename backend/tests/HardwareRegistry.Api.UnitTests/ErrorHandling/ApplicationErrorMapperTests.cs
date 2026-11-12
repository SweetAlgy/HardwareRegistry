using HardwareRegistry.Api.ErrorHandling;
using HardwareRegistry.Application.Common.Errors;
using HardwareRegistry.Domain.Devices;
using Microsoft.AspNetCore.Http;

namespace HardwareRegistry.Api.UnitTests.ErrorHandling
{
    public sealed class ApplicationErrorMapperTests
    {
        [Fact]
        public void ToProblemDetails_ConflictWithDetails_FillsEveryField()
        {
            var context = ApplicationErrorMapperTests.CreateContext("/api/devices");
            var error = EntityErrors<DeviceEntity>.AlreadyExists(nameof(DeviceEntity.InventoryNumber), "PC1");

            var problem = error.ToProblemDetails(context);

            Assert.Equal(409, problem.Status);
            Assert.Equal("Conflict", problem.Title);
            Assert.Equal(error.Message, problem.Detail);
            Assert.Equal("/api/devices", problem.Instance);
            Assert.Equal("Device.AlreadyExists", problem.Extensions["code"]);
            Assert.Equal("trace-1", problem.Extensions["traceId"]);
            Assert.Same(error.Details, problem.Extensions["details"]);
        }

        [Fact]
        public void ToProblemDetails_ErrorWithoutDetails_OmitsDetails()
        {
            var problem = new Error("Device.Invalid", "Bad.", ErrorType.Validation)
                .ToProblemDetails(ApplicationErrorMapperTests.CreateContext("/api/devices"));

            Assert.Equal(400, problem.Status);
            Assert.False(problem.Extensions.ContainsKey("details"));
        }

        private static DefaultHttpContext CreateContext(string path)
            => new()
            {
                Request = { Path = path },
                TraceIdentifier = "trace-1"
            };
    }
}
