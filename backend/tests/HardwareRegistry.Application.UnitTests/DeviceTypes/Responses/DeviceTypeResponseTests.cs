using HardwareRegistry.Application.DeviceTypes.Responses;
using HardwareRegistry.Domain.DeviceTypes;

namespace HardwareRegistry.Application.UnitTests.DeviceTypes.Responses
{
    public sealed class DeviceTypeResponseTests
    {
        [Fact]
        public void FromEntity_DeviceType_CopiesIdAndName()
        {
            var deviceType = new DeviceTypeEntity { Name = "Принтер" };

            var response = DeviceTypeResponse.FromEntity(deviceType);

            Assert.Equal(deviceType.Id, response.Id);
            Assert.Equal("Принтер", response.Name);
        }
    }
}
