using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HardwareRegistry.IntegrationTests.Infrastructure;

namespace HardwareRegistry.IntegrationTests.Api
{
    public sealed class DeviceTypesApiTests(TestEnvironment environment) : IntegrationTest(environment)
    {
        [Fact]
        public async Task Get_SeededDictionary_ReturnsTypesSortedByName()
        {
            var client = this.Environment.Factory.CreateClient();

            var response = await client.GetAsync("/api/device-types", IntegrationTest.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var types = await response.Content.ReadFromJsonAsync<JsonElement[]>(IntegrationTest.CancellationToken);
            Assert.NotNull(types);
            Assert.InRange(types.Length, 3, 5);
            var names = types.Select(type => type.GetProperty("name").GetString()).ToList();
            Assert.Equal(names.Order(StringComparer.Ordinal), names);
            Assert.All(types, type => Assert.NotEqual(Guid.Empty, type.GetProperty("id").GetGuid()));
        }
    }
}
