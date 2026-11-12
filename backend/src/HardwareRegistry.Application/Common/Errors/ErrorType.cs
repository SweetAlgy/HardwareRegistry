namespace HardwareRegistry.Application.Common.Errors
{
    /// <summary>
    /// Defines transport-independent categories used to classify application errors.
    /// </summary>
    public enum ErrorType
    {
        None,
        Validation,
        Unauthorized,
        Forbidden,
        NotFound,
        Gone,
        Conflict,
        PreconditionFailed,
        PayloadTooLarge,
        UnsupportedMediaType,
        RateLimited,
        Unavailable,
        Timeout,
        Failure
    }
}