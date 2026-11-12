using System.Net;
using System.Text.Json;
using HardwareRegistry.IntegrationTests.Infrastructure;

namespace HardwareRegistry.IntegrationTests.Api
{
    public sealed class SwaggerTests(TestEnvironment environment) : IntegrationTest(environment)
    {
        [Fact]
        public async Task Get_SwaggerDocument_DescribesEveryEndpoint()
        {
            var client = this.Environment.Factory.CreateClient();

            var response = await client.GetAsync("/swagger/v1/swagger.json", IntegrationTest.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(IntegrationTest.CancellationToken));
            var paths = document.RootElement.GetProperty("paths");
            Assert.True(paths.TryGetProperty("/api/devices", out _));
            Assert.True(paths.TryGetProperty("/api/devices/{deviceId}", out _));
            Assert.True(paths.TryGetProperty("/api/device-types", out _));
        }
    }
}
