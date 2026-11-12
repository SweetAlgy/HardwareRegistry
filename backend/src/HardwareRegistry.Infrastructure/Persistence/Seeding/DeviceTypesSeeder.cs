using HardwareRegistry.Application.Common;
using HardwareRegistry.Domain.DeviceTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HardwareRegistry.Infrastructure.Persistence.Seeding
{
    /// <summary>
    /// Fills the device types dictionary on server start when it is empty.
    /// </summary>
    internal sealed class DeviceTypesSeeder(
        ILoggerFactory loggerFactory,
        IServiceScopeFactory serviceScopeFactory
    ) : ServiceBase(loggerFactory), IHostedService
    {
        private static readonly string[] DefaultDeviceTypeNames =
        [
            "Системный блок",
            "Ноутбук",
            "Монитор",
            "Принтер",
            "Сетевое оборудование"
        ];

        private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await using var scope = this._serviceScopeFactory.CreateAsyncScope();

            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            if (await context.DeviceTypes.AnyAsync(cancellationToken))
                return;

            var deviceTypeEntities = DeviceTypesSeeder.DefaultDeviceTypeNames
                .Select(name => new DeviceTypeEntity { Name = name });

            context.DeviceTypes.AddRange(deviceTypeEntities);
            await context.SaveChangesAsync(cancellationToken);

            this.Logger.LogInformation(
                "Device types dictionary was empty: seeded {DeviceTypesCount} default types.",
                DeviceTypesSeeder.DefaultDeviceTypeNames.Length
            );
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
