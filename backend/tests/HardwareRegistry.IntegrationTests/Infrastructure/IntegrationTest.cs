using HardwareRegistry.Domain.Devices;
using HardwareRegistry.Domain.DeviceTypes;
using HardwareRegistry.Infrastructure.Persistence;
using HardwareRegistry.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HardwareRegistry.IntegrationTests.Infrastructure
{
    /// <summary>
    /// Every test starts from a database holding only the seeded device types dictionary.
    /// </summary>
    public abstract class IntegrationTest(TestEnvironment environment) : IAsyncLifetime
    {
        protected TestEnvironment Environment { get; } = environment;

        protected static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

        public virtual async ValueTask InitializeAsync()
        {
            await using var context = this.Environment.CreateDbContext();
            await context.Database.ExecuteSqlRawAsync("TRUNCATE devices, device_types");

            await this.RunSeederAsync();
        }

        public virtual ValueTask DisposeAsync() => ValueTask.CompletedTask;

        protected async Task RunSeederAsync()
        {
            var seeder = this.Environment.Factory.Services
                .GetServices<IHostedService>()
                .OfType<DeviceTypesSeeder>()
                .Single();

            await seeder.StartAsync(IntegrationTest.CancellationToken);
        }

        protected async Task<IReadOnlyList<DeviceTypeEntity>> GetDeviceTypesAsync()
        {
            await using var context = this.Environment.CreateDbContext();
            return await context.DeviceTypes.AsNoTracking().OrderBy(entity => entity.Name).ToListAsync();
        }

        protected async Task<DeviceEntity> AddDeviceAsync(
            string inventoryNumber,
            string name = "Workstation",
            int room = 1,
            DeviceTypeEntity? type = null
        )
        {
            type ??= (await this.GetDeviceTypesAsync())[0];

            var device = new DeviceEntity(DateTime.UtcNow)
            {
                InventoryNumber = inventoryNumber,
                Name = name,
                TypeId = type.Id,
                Room = room
            };

            await using var context = this.Environment.CreateDbContext();
            context.Devices.Add(device);
            await context.SaveChangesAsync();

            return device;
        }

        /// <summary>Fills the registry quickly, bypassing the API and its limit check.</summary>
        protected async Task AddDevicesAsync(int count)
        {
            var typeId = (await this.GetDeviceTypesAsync())[0].Id;

            await using var context = this.Environment.CreateDbContext();
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO devices (id, inventory_number, name, type_id, room, created_at, updated_at)
                SELECT gen_random_uuid(), 'BULK' || n, 'Bulk device ' || n, {typeId}, 1, now(), now()
                FROM generate_series(1, {count}) AS n
                """
            );
        }

        protected async Task<int> CountDevicesAsync()
        {
            await using var context = this.Environment.CreateDbContext();
            return await context.Devices.CountAsync();
        }

        protected async Task<T> WithScopeAsync<T>(Func<IServiceProvider, Task<T>> action)
        {
            await using var scope = this.Environment.CreateScope();
            return await action(scope.ServiceProvider);
        }

        protected async Task<T> WithContextAsync<T>(Func<ApplicationDbContext, Task<T>> action)
        {
            await using var context = this.Environment.CreateDbContext();
            return await action(context);
        }
    }
}
