using HardwareRegistry.Application.Devices.Abstractions;
using HardwareRegistry.Domain.Devices;
using HardwareRegistry.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace HardwareRegistry.IntegrationTests.Api
{
    public sealed class DeviceLimitConcurrencyTests(TestEnvironment environment) : IntegrationTest(environment)
    {
        [Fact]
        public async Task CreateDeviceAsync_ConcurrentRequestsForLastSlots_NeverExceedsLimit()
        {
            const int freeSlots = 3;
            const int concurrentRequests = 12;
            await this.AddDevicesAsync(DeviceConstants.MaxCount - freeSlots);
            var typeId = (await this.GetDeviceTypesAsync())[0].Id;

            var results = await Task.WhenAll(Enumerable.Range(0, concurrentRequests).Select(index =>
                    // A scope per request: separate DbContexts, separate connections, as with real traffic.
                    this.WithScopeAsync(services => services
                        .GetRequiredService<IDevicesService>()
                        .CreateDeviceAsync(
                            new() { InventoryNumber = $"RACE{index}", Name = "Race", TypeId = typeId, Room = 1 },
                            IntegrationTest.CancellationToken
                        )
                    )
                )
            );

            Assert.Equal(freeSlots, results.Count(result => result.IsSuccess));
            Assert.All(
                results.Where(result => result.IsFailure),
                result => Assert.Equal("Operation.LimitExceeded", result.Error.Code)
            );
            Assert.Equal(DeviceConstants.MaxCount, await this.CountDevicesAsync());
        }

        [Fact]
        public async Task CreateDeviceAsync_ConcurrentRequestsWithSameInventoryNumber_CreatesOne()
        {
            var typeId = (await this.GetDeviceTypesAsync())[0].Id;

            var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ =>
                    this.WithScopeAsync(services => services
                        .GetRequiredService<IDevicesService>()
                        .CreateDeviceAsync(
                            new() { InventoryNumber = "SAME1", Name = "Race", TypeId = typeId, Room = 1 },
                            IntegrationTest.CancellationToken
                        )
                    )
                )
            );

            Assert.Equal(1, results.Count(result => result.IsSuccess));
            Assert.All(
                results.Where(result => result.IsFailure),
                result => Assert.Equal("Device.AlreadyExists", result.Error.Code)
            );
            Assert.Equal(1, await this.CountDevicesAsync());
        }
    }
}
