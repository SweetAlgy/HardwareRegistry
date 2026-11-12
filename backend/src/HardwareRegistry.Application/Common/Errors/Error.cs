namespace HardwareRegistry.Application.Common.Errors
{
    public sealed record Error(
        string Code,
        string Message,
        ErrorType Type = ErrorType.None,
        IReadOnlyDictionary<string, object?>? Details = null
    )
    {
        /// <summary>
        /// A sentinel value representing the absence of an error, used in successful results.
        /// </summary>
        public static readonly Error None = new(String.Empty, String.Empty);
    }
}