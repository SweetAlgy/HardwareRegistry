using HardwareRegistry.Api.Extensions;
using HardwareRegistry.Application.Common;
using HardwareRegistry.Application.Common.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HardwareRegistry.Api.UnitTests.Extensions
{
    public sealed class ControllerBaseExtensionsTests
    {
        [Fact]
        public void Error_FailedResult_ReturnsProblemWithErrorStatus()
        {
            var controller = new TestController
            {
                ControllerContext = new() { HttpContext = new DefaultHttpContext() }
            };
            var result = Result.Failure(new Error("Device.NotFound", "Not found.", ErrorType.NotFound));

            var actionResult = controller.Error(result);

            Assert.Equal(StatusCodes.Status404NotFound, actionResult.StatusCode);
            Assert.Contains("application/problem+json", actionResult.ContentTypes);
            var problem = Assert.IsType<ProblemDetails>(actionResult.Value);
            Assert.Equal("Device.NotFound", problem.Extensions["code"]);
        }

        private sealed class TestController : ControllerBase;
    }
}
