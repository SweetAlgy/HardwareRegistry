using HardwareRegistry.Application.Common.Exceptions;
using HardwareRegistry.Domain.Devices;
using HardwareRegistry.Domain.DeviceTypes;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HardwareRegistry.Infrastructure.Persistence.Postgres
{
    internal static class PostgresExceptionMapper
    {
        public static Exception Map(DbUpdateException exception)
        {
            if (exception.InnerException is not PostgresException postgresException)
            {
                return exception;
            }

            if (postgresException.SqlState == PostgresErrorCodes.UniqueViolation)
                return PostgresExceptionMapper.MapViolation(exception, postgresException);

            return exception;
        }

        private static Exception MapViolation(DbUpdateException exception, PostgresException postgresException)
        {
            return postgresException.ConstraintName switch
            {
                ConstraintNames.DeviceInventoryNumberIndex => new PersistenceConflictException(
                    typeof(DeviceEntity),
                    PostgresExceptionMapper.GetValues<DeviceEntity>(exception, nameof(DeviceEntity.InventoryNumber)),
                    postgresException.ConstraintName,
                    exception
                ),
                ConstraintNames.DeviceTypeNameIndex => new PersistenceConflictException(
                    typeof(DeviceTypeEntity),
                    PostgresExceptionMapper.GetValues<DeviceTypeEntity>(exception, nameof(DeviceTypeEntity.Name)),
                    postgresException.ConstraintName,
                    exception
                ),
                _ => exception
            };
        }

        private static Dictionary<string, object?> GetValues<TEntity>(
            DbUpdateException exception,
            params string[] propertyNames
        )
        {
            var entry = exception.Entries.FirstOrDefault(entry => entry.Metadata.ClrType == typeof(TEntity));

            if (entry is null)
            {
                return propertyNames.ToDictionary(
                    propertyName => propertyName,
                    object? (_) => null
                );
            }

            return propertyNames.ToDictionary(
                propertyName => propertyName,
                propertyName => entry.Property(propertyName).CurrentValue
            );
        }
    }
}
