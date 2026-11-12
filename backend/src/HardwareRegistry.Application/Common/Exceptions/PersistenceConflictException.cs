namespace HardwareRegistry.Application.Common.Exceptions
{
    public sealed class PersistenceConflictException(
        Type entityType,
        IReadOnlyDictionary<string, object?> conflictingValues,
        string? constraintName = null,
        Exception? innerException = null
    )
        : Exception(
            PersistenceConflictException.BuildMessage(
                entityType,
                conflictingValues,
                constraintName
            ),
            innerException
        )
    {
        public PersistenceConflictException(
            Type entityType,
            string conflictingFieldName,
            object? conflictingFieldValue,
            string? constraintName = null,
            Exception? innerException = null
        )
            : this(
                entityType,
                new Dictionary<string, object?> { { conflictingFieldName, conflictingFieldValue } },
                constraintName,
                innerException
            )
        { }

        public Type EntityType { get; init; } = entityType;
        public string EntityTypeName => this.EntityType.Name;

        public IReadOnlyDictionary<string, object?> ConflictingValues { get; } = conflictingValues;

        public string? ConstraintName { get; } = constraintName;

        private static string BuildMessage(
            Type entityType,
            IReadOnlyDictionary<string, object?> conflictingValues,
            string? constraintName
        )
        {
            var values = String.Join(
                ", ",
                conflictingValues.Select(pair => $"{pair.Key}: {pair.Value}")
            );

            return constraintName is null
                ? $"{entityType.Name} already exists. Conflicting values: {values}."
                : $"{entityType.Name} already exists. Constraint: {constraintName}. Conflicting values: {values}.";
        }
    }
}