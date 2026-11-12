using Humanizer;
using HardwareRegistry.Application.Common.Formatting;
using HardwareRegistry.Domain.Common.Abstractions;

namespace HardwareRegistry.Application.Common.Errors
{
    public static class EntityErrors<TEntity>
    {
        private static string EntityCode { get; } =
            typeof(TEntity).Name.Replace("Entity", String.Empty);

        private static string EntityName { get; } =
            typeof(TEntity).Humanize();

        private static string EntityNamePlural { get; } =
            typeof(TEntity).HumanizePlural();

        public static Error NotFound(string? message = null)
            => new(
                $"{EntityErrors<TEntity>.EntityCode}.NotFound",
                message ?? $"{EntityErrors<TEntity>.EntityName} was not found.",
                ErrorType.NotFound
            );

        public static Error NotFoundBy<TValue>(string fieldName, TValue fieldValue)
            => new(
                $"{EntityErrors<TEntity>.EntityCode}.NotFound",
                $"{
                    EntityErrors<TEntity>.EntityName
                } with {
                    fieldName.Humanize().ToLowerInvariant()
                }: '{
                    fieldValue
                }' was not found.",
                ErrorType.NotFound,
                new Dictionary<string, object?>
                {
                    [fieldName] = fieldValue
                }
            );

        public static Error NotFoundById<TId>(TId id)
            => EntityErrors<TEntity>.NotFoundBy(nameof(IEntity<>.Id), id);

public static Error NotFoundById(Guid id)
            => EntityErrors<TEntity>.NotFoundBy(nameof(IEntity.Id), id);

        public static Error NoneFound(string? message = null)
            => new(
                $"{EntityErrors<TEntity>.EntityCode}.NoneFound",
                message ?? $"No {EntityErrors<TEntity>.EntityNamePlural} were found.",
                ErrorType.NotFound
            );

        public static Error NoneFoundBy<TValue>(string fieldName, IReadOnlyCollection<TValue> fieldValues)
        {
            var valuesDescription = String.Join(
                ", ",
                fieldValues.Select(value => $"'{value}'")
            );

            return new(
                $"{EntityErrors<TEntity>.EntityCode}.NoneFound",
                $"No {
                    EntityErrors<TEntity>.EntityNamePlural
                } with {
                    fieldName.Humanize().ToLowerInvariant()
                }: {
                    valuesDescription
                } were found.",
                ErrorType.NotFound,
                new Dictionary<string, object?>
                {
                    [fieldName] = fieldValues
                }
            );
        }

        public static Error NoneFoundByIds<TId>(IReadOnlyCollection<TId> ids)
            => EntityErrors<TEntity>.NoneFoundBy(nameof(IEntity<>.Id), ids);

        public static Error NoneFoundByIds(IReadOnlyCollection<Guid> ids)
            => EntityErrors<TEntity>.NoneFoundBy(nameof(IEntity.Id), ids);

        public static Error Invalid(string message, IReadOnlyDictionary<string, object?>? details = null)
            => new(
                $"{EntityErrors<TEntity>.EntityCode}.Invalid",
                message,
                ErrorType.Validation,
                details
            );

        public static Error Conflict(string message, IReadOnlyDictionary<string, object?>? details = null)
            => new(
                $"{EntityErrors<TEntity>.EntityCode}.Conflict",
                message,
                ErrorType.Conflict,
                details
            );

        public static Error AlreadyExists<TValue>(string fieldName, TValue fieldValue)
            => new(
                $"{EntityErrors<TEntity>.EntityCode}.AlreadyExists",
                $"{
                    EntityErrors<TEntity>.EntityName
                } with {
                    fieldName.Humanize().ToLowerInvariant()
                }: '{
                    fieldValue
                }' already exists.",
                ErrorType.Conflict,
                new Dictionary<string, object?>
                {
                    [fieldName] = fieldValue
                }
            );

        public static Error AlreadyExists(IReadOnlyDictionary<string, object?> conflictingValues)
        {
            var valuesDescription = String.Join(
                ", ",
                conflictingValues.Select(pair => $"{pair.Key}: '{pair.Value ?? "null"}'"
                )
            );

            return new(
                $"{EntityErrors<TEntity>.EntityCode}.AlreadyExists",
                $"{EntityErrors<TEntity>.EntityName} with {valuesDescription} already exists.",
                ErrorType.Conflict,
                conflictingValues
            );
        }

        public static Error Failure(string message, IReadOnlyDictionary<string, object?>? details = null)
            => new(
                $"{EntityErrors<TEntity>.EntityCode}.Failure",
                message,
                ErrorType.Failure,
                details
            );
    }
}