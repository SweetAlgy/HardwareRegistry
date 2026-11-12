using HardwareRegistry.Application.Common.Extensions;

namespace HardwareRegistry.Application.UnitTests.Common.Extensions
{
    public sealed class StringNamingConventionTransformerTests
    {
        [Theory]
        [InlineData("Devices", "devices")]
        [InlineData("DeviceTypes", "device_types")]
        [InlineData("inventoryNumber", "inventory_number")]
        [InlineData("already_snake", "already_snake")]
        [InlineData("", "")]
        public void ToSnakeCase_Input_ReturnsSnakeCase(string value, string expected)
            => Assert.Equal(expected, value.ToSnakeCase());
    }
}
