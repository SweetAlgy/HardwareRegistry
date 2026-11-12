using HardwareRegistry.Domain.DeviceTypes;
using HardwareRegistry.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HardwareRegistry.IntegrationTests.Persistence
{
    public sealed class DeviceTypesSeederTests(TestEnvironment environment) : IntegrationTest(environment)
    {
        [Fact]
        public async Task StartAsync_EmptyDictionary_SeedsThreeToFiveTypes()
        {
            await this.ClearDeviceTypesAsync();

            await this.RunSeederAsync();

            var names = (await this.GetDeviceTypesAsync()).Select(type => type.Name).ToList();
            Assert.InRange(names.Count, 3, 5);
            Assert.Equal(names.Count, names.Distinct().Count());
            Assert.Contains("Ноутбук", names);
        }

        [Fact]
        public async Task StartAsync_DictionaryNotEmpty_LeavesItUntouched()
        {
            await this.ClearDeviceTypesAsync();
            await this.WithContextAsync(async context =>
                {
                    context.DeviceTypes.Add(new DeviceTypeEntity { Name = "Сканер" });
                    return await context.SaveChangesAsync(IntegrationTest.CancellationToken);
                }
            );

            await this.RunSeederAsync();

            var type = Assert.Single(await this.GetDeviceTypesAsync());
            Assert.Equal("Сканер", type.Name);
        }

        [Fact]
        public async Task StartAsync_RunTwice_DoesNotDuplicate()
        {
            var countBefore = (await this.GetDeviceTypesAsync()).Count;

            await this.RunSeederAsync();

            Assert.Equal(countBefore, (await this.GetDeviceTypesAsync()).Count);
        }

        private Task ClearDeviceTypesAsync()
            => this.WithContextAsync(context =>
                context.Database.ExecuteSqlRawAsync("TRUNCATE devices, device_types", IntegrationTest.CancellationToken)
            );
    }
}
