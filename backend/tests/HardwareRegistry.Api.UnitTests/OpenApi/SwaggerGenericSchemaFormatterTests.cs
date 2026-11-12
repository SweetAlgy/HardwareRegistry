using HardwareRegistry.Api.OpenApi;
using HardwareRegistry.Application.Common;
using HardwareRegistry.Application.Devices.Responses;

namespace HardwareRegistry.Api.UnitTests.OpenApi
{
    public sealed class SwaggerGenericSchemaFormatterTests
    {
        [Theory]
        [InlineData(typeof(DeviceResponse), "DeviceResponse")]
        [InlineData(typeof(PagedResult<DeviceResponse>), "PagedResult<DeviceResponse>")]
        [InlineData(typeof(Dictionary<string, List<int>>), "Dictionary<String, List<Int32>>")]
        public void Format_Type_ReturnsReadableName(Type type, string expected)
            => Assert.Equal(expected, SwaggerGenericSchemaFormatter.Format(type));
    }
}
