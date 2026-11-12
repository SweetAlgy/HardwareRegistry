namespace HardwareRegistry.Application.Common.Errors
{
    public static class RequestErrors
    {
        public static Error Invalid(string message, IReadOnlyDictionary<string, object?>? details = null)
            => new(
                "Request.Invalid",
                message,
                ErrorType.Validation,
                details
            );

        public static Error Empty(string? message = null)
            => new(
                "Request.Empty",
                message ?? "The request is empty.",
                ErrorType.Validation
            );

        public static Error Malformed(string message = "The request could not be parsed.")
            => new(
                "Request.Malformed",
                message,
                ErrorType.Validation
            );

        public static Error PayloadTooLarge(long? maximumSize = null)
            => new(
                "Request.PayloadTooLarge",
                maximumSize is null
                    ? "The request payload is too large."
                    : $"The request payload exceeds the maximum size of {maximumSize} bytes.",
                ErrorType.PayloadTooLarge,
                maximumSize is null
                    ? null
                    : new Dictionary<string, object?>
                    {
                        ["maximumSize"] = maximumSize
                    }
            );

        public static Error UnsupportedMediaType(
            string? mediaType = null,
            IReadOnlyCollection<string>? supportedMediaTypes = null
        ) => new(
            "Request.UnsupportedMediaType",
            mediaType is null
                ? "The request media type is not supported."
                : $"The media type '{mediaType}' is not supported.",
            ErrorType.UnsupportedMediaType,
            new Dictionary<string, object?>
            {
                ["mediaType"] = mediaType,
                ["supportedMediaTypes"] = supportedMediaTypes
            }
        );

        public static Error RateLimitExceeded(TimeSpan? retryAfter = null)
            => new(
                "Request.RateLimitExceeded",
                "Too many requests were made. Try again later.",
                ErrorType.RateLimited,
                retryAfter is null
                    ? null
                    : new Dictionary<string, object?>
                    {
                        ["retryAfterSeconds"] = retryAfter.Value.TotalSeconds
                    }
            );
    }
}