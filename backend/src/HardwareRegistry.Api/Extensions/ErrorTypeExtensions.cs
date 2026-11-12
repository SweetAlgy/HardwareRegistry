using HardwareRegistry.Application.Common.Errors;

namespace HardwareRegistry.Api.Extensions
{
    public static class ErrorTypeExtensions
    {
        extension(ErrorType errorType)
        {
            public int ToStatusCode() => errorType switch
            {
                ErrorType.Validation => StatusCodes.Status400BadRequest,
                ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
                ErrorType.Forbidden => StatusCodes.Status403Forbidden,
                ErrorType.NotFound => StatusCodes.Status404NotFound,
                ErrorType.Gone => StatusCodes.Status410Gone,
                ErrorType.Conflict => StatusCodes.Status409Conflict,
                ErrorType.PreconditionFailed => StatusCodes.Status412PreconditionFailed,
                ErrorType.PayloadTooLarge => StatusCodes.Status413PayloadTooLarge,
                ErrorType.UnsupportedMediaType => StatusCodes.Status415UnsupportedMediaType,
                ErrorType.RateLimited => StatusCodes.Status429TooManyRequests,
                ErrorType.Unavailable => StatusCodes.Status503ServiceUnavailable,
                ErrorType.Timeout => StatusCodes.Status504GatewayTimeout,
                ErrorType.Failure => StatusCodes.Status500InternalServerError,
                ErrorType.None => StatusCodes.Status200OK,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(errorType),
                    errorType,
                    "Unsupported error type."
                )
            };

            public string ToTitleString() => errorType switch
            {
                ErrorType.None => "None",
                ErrorType.Validation => "Bad Request",
                ErrorType.Unauthorized => "Unauthorized",
                ErrorType.Forbidden => "Forbidden",
                ErrorType.NotFound => "Resource not found",
                ErrorType.Gone => "Gone",
                ErrorType.Conflict => "Conflict",
                ErrorType.PreconditionFailed => "Precondition failed",
                ErrorType.PayloadTooLarge => "Payload too large",
                ErrorType.UnsupportedMediaType => "Unsupported media type",
                ErrorType.RateLimited => "Rate limited",
                ErrorType.Unavailable => "Service unavailable",
                ErrorType.Timeout => "Timeout",
                ErrorType.Failure => "Internal server error",
                _ => throw new ArgumentOutOfRangeException(nameof(errorType), errorType, null)
            };
        }
    }
}