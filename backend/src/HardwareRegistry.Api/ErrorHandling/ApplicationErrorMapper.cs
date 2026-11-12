using Microsoft.AspNetCore.Mvc;
using HardwareRegistry.Api.Extensions;
using HardwareRegistry.Application.Common.Errors;

namespace HardwareRegistry.Api.ErrorHandling
{
    public static class ApplicationErrorMapper
    {
        extension(Error error)
        {
            public ProblemDetails ToProblemDetails(HttpContext context)
            {
                var statusCode = error.Type.ToStatusCode();

                var problem = new ProblemDetails
                {
                    Status = statusCode,
                    Title = error.Type.ToTitleString(),
                    Detail = error.Message,
                    Instance = context.Request.Path
                };

                problem.Extensions.Add("code", error.Code);
                problem.Extensions.Add("traceId", context.TraceIdentifier);

                if (error.Details is not null)
                    problem.Extensions.Add("details", error.Details);

                return problem;
            }
        }
    }
}