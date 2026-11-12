using HardwareRegistry.Domain.DeviceTypes;

namespace HardwareRegistry.Domain.UnitTests.Common
{
    public sealed class EntityTests
    {
        [Fact]
        public void Constructor_NoId_GeneratesVersion7Id()
        {
            var deviceType = new DeviceTypeEntity { Name = "Монитор" };

            Assert.NotEqual(Guid.Empty, deviceType.Id);
            Assert.Equal(7, deviceType.Id.Version);
        }
    }
}
