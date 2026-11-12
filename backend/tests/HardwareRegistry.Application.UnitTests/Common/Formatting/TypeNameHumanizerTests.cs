using HardwareRegistry.Application.Common.Formatting;
using HardwareRegistry.Application.Devices.Requests;
using HardwareRegistry.Application.Devices.Responses;
using HardwareRegistry.Domain.Devices;
using HardwareRegistry.Domain.DeviceTypes;

namespace HardwareRegistry.Application.UnitTests.Common.Formatting
{
    public sealed class TypeNameHumanizerTests
    {
        [Theory]
        [InlineData(typeof(DeviceEntity), "Device")]
        [InlineData(typeof(DeviceTypeEntity), "Device type")]
        [InlineData(typeof(DeviceResponse), "Device")]
        [InlineData(typeof(SaveDeviceRequest), "Save device")]
        public void Humanize_Type_StripsSuffixesAndSplitsWords(Type type, string expected)
            => Assert.Equal(expected, type.Humanize());

        [Fact]
        public void HumanizePlural_EntityType_ReturnsPluralName()
            => Assert.Equal("Device types", typeof(DeviceTypeEntity).HumanizePlural());
    }
}
