using HardwareRegistry.Application.Common;
using HardwareRegistry.Application.Devices;
using HardwareRegistry.Application.Devices.Requests;
using HardwareRegistry.Domain.Devices;
using HardwareRegistry.Infrastructure.Persistence.Repositories;
using HardwareRegistry.IntegrationTests.Infrastructure;

namespace HardwareRegistry.IntegrationTests.Persistence
{
    public sealed class DevicesRepositoryTests(TestEnvironment environment) : IntegrationTest(environment)
    {
        [Fact]
        public async Task GetPageAsync_SortByInventoryNumber_ComparesNumbersNaturally()
        {
            foreach (var inventoryNumber in (string[])["PC10", "PC2", "PC1", "PC100", "AB3"])
                await this.AddDeviceAsync(inventoryNumber);

            var page = await this.GetPageAsync(new() { SortBy = DeviceSortField.InventoryNumber });

            Assert.Equal(["AB3", "PC1", "PC2", "PC10", "PC100"], page.Items.Select(device => device.InventoryNumber));
        }

        [Fact]
        public async Task GetPageAsync_SortByRoomDescending_OrdersByRoomDescending()
        {
            await this.AddDeviceAsync("A1", room: 5);
            await this.AddDeviceAsync("A2", room: 1000);
            await this.AddDeviceAsync("A3", room: 1);

            var page = await this.GetPageAsync(new() { SortBy = DeviceSortField.Room, SortOrder = SortOrder.Descending });

            Assert.Equal([1000, 5, 1], page.Items.Select(device => device.Room));
        }

        [Fact]
        public async Task GetPageAsync_SortByName_OrdersByName()
        {
            await this.AddDeviceAsync("A1", name: "Сканер");
            await this.AddDeviceAsync("A2", name: "Монитор 27");
            await this.AddDeviceAsync("A3", name: "Монитор 3");

            var page = await this.GetPageAsync(new() { SortBy = DeviceSortField.Name });

            Assert.Equal(["Монитор 3", "Монитор 27", "Сканер"], page.Items.Select(device => device.Name));
        }

        [Fact]
        public async Task GetPageAsync_SortByTypeName_OrdersByDictionaryName()
        {
            var types = await this.GetDeviceTypesAsync();
            foreach (var (type, index) in types.Reverse().Select((type, index) => (type, index)))
                await this.AddDeviceAsync($"A{index}", type: type);

            var page = await this.GetPageAsync(new() { SortBy = DeviceSortField.TypeName });

            Assert.Equal(types.Select(type => type.Name), page.Items.Select(device => device.Type!.Name));
        }

        [Fact]
        public async Task GetPageAsync_EqualSortValues_PagesWithoutDuplicatesOrGaps()
        {
            await this.AddDevicesAsync(45);

            var ids = new List<Guid>();
            for (var pageNumber = 1; pageNumber <= 5; pageNumber++)
            {
                var page = await this.GetPageAsync(
                    new() { SortBy = DeviceSortField.Room, Page = pageNumber, PageSize = 10 }
                );
                ids.AddRange(page.Items.Select(device => device.Id));
            }

            Assert.Equal(45, ids.Count);
            Assert.Equal(45, ids.Distinct().Count());
        }

        [Fact]
        public async Task GetPageAsync_SecondPage_ReturnsRemainderAndTotal()
        {
            await this.AddDevicesAsync(25);

            var page = await this.GetPageAsync(new() { Page = 2, PageSize = 20 });

            Assert.Equal(5, page.Items.Count());
            Assert.Equal(25, page.TotalCount);
            Assert.Equal(2, page.Page);
            Assert.Equal(20, page.PageSize);
        }

        [Fact]
        public async Task GetPageAsync_PageBeyondEnd_ReturnsNoItemsAndTotal()
        {
            await this.AddDevicesAsync(3);

            var page = await this.GetPageAsync(new() { Page = 10, PageSize = 20 });

            Assert.Empty(page.Items);
            Assert.Equal(3, page.TotalCount);
        }

        [Fact]
        public async Task GetPageAsync_EmptyRegistry_ReturnsEmptyPage()
        {
            var page = await this.GetPageAsync(new());

            Assert.Empty(page.Items);
            Assert.Equal(0, page.TotalCount);
        }

        [Fact]
        public async Task GetPageAsync_Devices_IncludesType()
        {
            await this.AddDeviceAsync("A1");

            var page = await this.GetPageAsync(new());

            Assert.NotNull(Assert.Single(page.Items).Type);
        }

        [Fact]
        public async Task GetByIdAsync_ExistingDevice_ReturnsItWithType()
        {
            var device = await this.AddDeviceAsync("A1", name: "Laptop", room: 42);

            var found = await this.WithContextAsync(context =>
                new DevicesRepository(context).GetByIdAsync(device.Id, IntegrationTest.CancellationToken)
            );

            Assert.NotNull(found);
            Assert.Equal("A1", found.InventoryNumber);
            Assert.Equal("Laptop", found.Name);
            Assert.Equal(42, found.Room);
            Assert.NotNull(found.Type);
        }

        [Fact]
        public async Task GetByIdAsync_MissingDevice_ReturnsNull()
        {
            var found = await this.WithContextAsync(context =>
                new DevicesRepository(context).GetByIdAsync(Guid.NewGuid(), IntegrationTest.CancellationToken)
            );

            Assert.Null(found);
        }

        [Fact]
        public async Task CountAsync_Devices_ReturnsTotal()
        {
            await this.AddDevicesAsync(7);

            var count = await this.WithContextAsync(context =>
                new DevicesRepository(context).CountAsync(IntegrationTest.CancellationToken)
            );

            Assert.Equal(7, count);
        }

        [Fact]
        public async Task DeleteByIdAsync_ExistingDevice_DeletesOnlyItAndReturnsTrue()
        {
            var device = await this.AddDeviceAsync("A1");
            await this.AddDeviceAsync("A2");

            var isDeleted = await this.WithContextAsync(context =>
                new DevicesRepository(context).DeleteByIdAsync(device.Id, IntegrationTest.CancellationToken)
            );

            Assert.True(isDeleted);
            Assert.Equal(1, await this.CountDevicesAsync());
        }

        [Fact]
        public async Task DeleteByIdAsync_MissingDevice_ReturnsFalse()
        {
            await this.AddDeviceAsync("A1");

            var isDeleted = await this.WithContextAsync(context =>
                new DevicesRepository(context).DeleteByIdAsync(Guid.NewGuid(), IntegrationTest.CancellationToken)
            );

            Assert.False(isDeleted);
            Assert.Equal(1, await this.CountDevicesAsync());
        }

        private Task<PagedResult<DeviceEntity>> GetPageAsync(GetDevicesRequest request)
            => this.WithContextAsync(context =>
                new DevicesRepository(context).GetPageAsync(request, IntegrationTest.CancellationToken)
            );
    }
}
