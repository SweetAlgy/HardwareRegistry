using HardwareRegistry.Application.Devices.Responses;
using HardwareRegistry.Domain.Devices;
using HardwareRegistry.Domain.DeviceTypes;

namespace HardwareRegistry.Application.UnitTests.Devices.Responses
{
    public sealed class DeviceResponseTests
    {
        [Fact]
        public void FromEntity_DeviceWithType_CopiesEveryField()
        {
            var createdAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var deviceType = new DeviceTypeEntity { Name = "Монитор" };
            var device = new DeviceEntity(Guid.NewGuid(), createdAt, createdAt.AddDays(1))
            {
                InventoryNumber = "MON7",
                Name = "Dell P2422H",
                TypeId = deviceType.Id,
                Type = deviceType,
                Room = 305
            };

            var response = DeviceResponse.FromEntity(device);

            Assert.Equal(device.Id, response.Id);
            Assert.Equal(createdAt, response.CreatedAt);
            Assert.Equal(createdAt.AddDays(1), response.UpdatedAt);
            Assert.Equal("MON7", response.InventoryNumber);
            Assert.Equal("Dell P2422H", response.Name);
            Assert.Equal(deviceType.Id, response.TypeId);
            Assert.Equal("Монитор", response.TypeName);
            Assert.Equal(305, response.Room);
        }

        [Fact]
        public void FromEntity_TypeNotLoaded_ReturnsEmptyTypeName()
        {
            var device = new DeviceEntity(DateTime.UtcNow)
            {
                InventoryNumber = "MON7",
                Name = "Dell P2422H",
                TypeId = Guid.NewGuid(),
                Room = 305
            };

            var response = DeviceResponse.FromEntity(device);

            Assert.Equal(String.Empty, response.TypeName);
        }
    }
}
