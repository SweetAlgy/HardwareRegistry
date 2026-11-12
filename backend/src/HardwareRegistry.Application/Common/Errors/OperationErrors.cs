namespace HardwareRegistry.Application.Common.Errors
{
    public static class OperationErrors
    {
        public static Error NotAllowed(string operation, string? message = null)
            => new(
                "Operation.NotAllowed",
                message ?? $"The operation '{operation}' is not allowed in the current state.",
                ErrorType.Conflict,
                new Dictionary<string, object?>
                {
                    ["operation"] = operation
                }
            );

        public static Error AlreadyInProgress(string operation)
            => new(
                "Operation.AlreadyInProgress",
                $"The operation '{operation}' is already in progress.",
                ErrorType.Conflict,
                new Dictionary<string, object?>
                {
                    ["operation"] = operation
                }
            );

        public static Error ConcurrentModification()
            => new(
                "Operation.ConcurrentModification",
                "The resource was modified by another operation.",
                ErrorType.Conflict
            );

        public static Error PreconditionFailed(string message, IReadOnlyDictionary<string, object?>? details = null)
            => new(
                "Operation.PreconditionFailed",
                message,
                ErrorType.PreconditionFailed,
                details
            );

        public static Error IdempotencyConflict(string idempotencyKey)
            => new(
                "Operation.IdempotencyConflict",
                "The idempotency key has already been used with different request data.",
                ErrorType.Conflict,
                new Dictionary<string, object?>
                {
                    ["idempotencyKey"] = idempotencyKey
                }
            );

        public static Error Expired(string operation, DateTimeOffset? expiredAt = null)
            => new(
                "Operation.Expired",
                $"The operation '{operation}' has expired.",
                ErrorType.Gone,
                new Dictionary<string, object?>
                {
                    ["operation"] = operation,
                    ["expiredAt"] = expiredAt
                }
            );

        public static Error FeatureDisabled(string feature)
            => new(
                "Operation.FeatureDisabled",
                "The requested feature is currently disabled.",
                ErrorType.Unavailable,
                new Dictionary<string, object?>
                {
                    ["feature"] = feature
                }
            );

        public static Error LimitExceeded(string limit, object? maximum = null)
            => new(
                "Operation.LimitExceeded",
                $"The '{limit}' limit has been exceeded.",
                ErrorType.Validation,
                new Dictionary<string, object?>
                {
                    ["limit"] = limit,
                    ["maximum"] = maximum
                }
            );
    }
}