using Microsoft.AspNetCore.Mvc;
using HardwareRegistry.Api.ErrorHandling;
using HardwareRegistry.Application.Common;

namespace HardwareRegistry.Api.Extensions
{
    internal static class ControllerBaseExtensions
    {
        extension(ControllerBase controller)
        {
            public ObjectResult Error(Result result)
            {
                var problem = result.Error.ToProblemDetails(controller.HttpContext);
                return new(problem) { StatusCode = problem.Status, ContentTypes = { "application/problem+json" } };
            }
        }
    }
}