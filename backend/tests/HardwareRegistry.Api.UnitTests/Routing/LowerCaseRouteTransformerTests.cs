using HardwareRegistry.Api.Routing;

namespace HardwareRegistry.Api.UnitTests.Routing
{
    public sealed class LowerCaseRouteTransformerTests
    {
        [Theory]
        [InlineData("Devices", "devices")]
        [InlineData("DeviceTypes", "device_types")]
        [InlineData(null, "")]
        public void TransformOutbound_RouteValue_ReturnsSnakeCase(string? value, string expected)
            => Assert.Equal(expected, new LowerCaseRouteTransformer().TransformOutbound(value));
    }
}
