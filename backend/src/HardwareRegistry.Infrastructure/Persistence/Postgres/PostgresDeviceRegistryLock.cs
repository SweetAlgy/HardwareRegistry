using System.Text;
using HardwareRegistry.Application.Devices.Abstractions;

namespace HardwareRegistry.Infrastructure.Persistence.Postgres
{
    public sealed class PostgresDeviceRegistryLock(ApplicationDbContext context) : IDeviceRegistryLock
    {
        private const string LockName = "device-registry";

        private static readonly AdvisoryLockKey Key = AdvisoryLockKey.From(
            Encoding.UTF8.GetBytes(PostgresDeviceRegistryLock.LockName)
        );

        private readonly ApplicationDbContext _context = context;

        public async Task AcquireAsync(CancellationToken cancellationToken = default)
            => await PostgresAdvisoryLock.AcquireAsync(
                this._context.Database,
                PostgresDeviceRegistryLock.Key,
                PostgresDeviceRegistryLock.LockName,
                cancellationToken
            );
    }
}
