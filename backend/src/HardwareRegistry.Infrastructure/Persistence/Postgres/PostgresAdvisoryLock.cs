using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace HardwareRegistry.Infrastructure.Persistence.Postgres
{
    /// <summary>
    /// Transaction-scoped PostgreSQL advisory locks. The lock is released when the surrounding
    /// transaction ends, so a caller can neither forget to unlock nor leak one by throwing.
    /// </summary>
    internal static class PostgresAdvisoryLock
    {
        public static async Task AcquireAsync(
            DatabaseFacade database,
            AdvisoryLockKey key,
            string lockName,
            CancellationToken cancellationToken = default
        )
        {
            if (database.CurrentTransaction is null)
                throw new InvalidOperationException($"A {lockName} lock requires an active database transaction.");

            await database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock({key.High}, {key.Low});",
                cancellationToken
            );
        }
    }
}