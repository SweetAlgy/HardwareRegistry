using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HardwareRegistry.Domain.Devices;
using HardwareRegistry.IntegrationTests.Infrastructure;

namespace HardwareRegistry.IntegrationTests.Api
{
    public sealed class DevicesApiTests(TestEnvironment environment) : IntegrationTest(environment)
    {
        private readonly HttpClient _client = environment.Factory.CreateClient();

        [Fact]
        public async Task Post_ValidCard_Returns201WithLocationAndDevice()
        {
            var typeId = await this.GetTypeIdAsync();

            var response = await this.PostAsync(new { inventoryNumber = "PC1", name = " Рабочая станция ", typeId, room = 101 });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.NotNull(response.Headers.Location);
            var device = await DevicesApiTests.ReadJsonAsync(response);
            Assert.Equal("PC1", device.GetProperty("inventoryNumber").GetString());
            Assert.Equal("Рабочая станция", device.GetProperty("name").GetString());
            Assert.Equal(typeId, device.GetProperty("typeId").GetGuid());
            Assert.False(String.IsNullOrEmpty(device.GetProperty("typeName").GetString()));
            Assert.Equal(101, device.GetProperty("room").GetInt32());

            var fetched = await this._client.GetAsync(response.Headers.Location, IntegrationTest.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, fetched.StatusCode);
            Assert.Equal(device.GetProperty("id").GetGuid(), (await DevicesApiTests.ReadJsonAsync(fetched)).GetProperty("id").GetGuid());
        }

        [Theory]
        [InlineData("PC-1", "Name", 1, "InventoryNumber")]
        [InlineData("ПК1", "Name", 1, "InventoryNumber")]
        [InlineData("", "Name", 1, "InventoryNumber")]
        [InlineData("A1B2C3D4E5F6G7H8I9J0K1L2M3N4O5P6Q", "Name", 1, "InventoryNumber")]
        [InlineData("PC1", "", 1, "Name")]
        [InlineData("PC1", "Name", 0, "Room")]
        [InlineData("PC1", "Name", 1001, "Room")]
        public async Task Post_InvalidCard_Returns400NamingTheField(
            string inventoryNumber,
            string name,
            int room,
            string expectedField
        )
        {
            var typeId = await this.GetTypeIdAsync();

            var response = await this.PostAsync(new { inventoryNumber, name, typeId, room });

            await DevicesApiTests.AssertValidationErrorAsync(response, expectedField);
            Assert.Equal(0, await this.CountDevicesAsync());
        }

        [Fact]
        public async Task Post_NameLongerThan256_Returns400()
        {
            var typeId = await this.GetTypeIdAsync();

            var response = await this.PostAsync(new { inventoryNumber = "PC1", name = new string('Я', 257), typeId, room = 1 });

            await DevicesApiTests.AssertValidationErrorAsync(response, "Name");
        }

        [Fact]
        public async Task Post_TypeAndRoomOmitted_Returns400NamingBoth()
        {
            var response = await this.PostAsync(new { inventoryNumber = "PC1", name = "Name" });

            var errors = await DevicesApiTests.AssertValidationErrorAsync(response, "$");
            var message = errors.GetProperty("$")[0].GetString();
            Assert.Contains("typeId", message);
            Assert.Contains("room", message);
        }

        [Fact]
        public async Task Post_TypeAndRoomNull_Returns400NamingBoth()
        {
            var response = await this.PostAsync(new { inventoryNumber = "PC1", name = "Name", typeId = (Guid?)null, room = (int?)null });

            var errors = await DevicesApiTests.AssertValidationErrorAsync(response, "TypeId");
            Assert.True(errors.TryGetProperty("Room", out _));
        }

        [Fact]
        public async Task Post_FractionalRoom_Returns400()
        {
            var typeId = await this.GetTypeIdAsync();

            var response = await this.PostAsync(new { inventoryNumber = "PC1", name = "Name", typeId, room = 1.5 });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Post_UnknownType_Returns400DeviceInvalid()
        {
            var response = await this.PostAsync(new { inventoryNumber = "PC1", name = "Name", typeId = Guid.NewGuid(), room = 1 });

            await DevicesApiTests.AssertProblemAsync(response, HttpStatusCode.BadRequest, "Device.Invalid");
        }

        [Fact]
        public async Task Post_DuplicateInventoryNumber_Returns409DeviceAlreadyExists()
        {
            await this.AddDeviceAsync("PC1");
            var typeId = await this.GetTypeIdAsync();

            var response = await this.PostAsync(new { inventoryNumber = "PC1", name = "Other", typeId, room = 2 });

            await DevicesApiTests.AssertProblemAsync(response, HttpStatusCode.Conflict, "Device.AlreadyExists");
            Assert.Equal(1, await this.CountDevicesAsync());
        }

        [Fact]
        public async Task Post_RegistryFull_Returns400LimitExceeded()
        {
            await this.AddDevicesAsync(DeviceConstants.MaxCount);
            var typeId = await this.GetTypeIdAsync();

            var response = await this.PostAsync(new { inventoryNumber = "PC1", name = "Name", typeId, room = 1 });

            await DevicesApiTests.AssertProblemAsync(response, HttpStatusCode.BadRequest, "Operation.LimitExceeded");
            Assert.Equal(DeviceConstants.MaxCount, await this.CountDevicesAsync());
        }

        [Fact]
        public async Task Post_LastFreeSlot_Returns201()
        {
            await this.AddDevicesAsync(DeviceConstants.MaxCount - 1);
            var typeId = await this.GetTypeIdAsync();

            var response = await this.PostAsync(new { inventoryNumber = "PC1", name = "Name", typeId, room = 1 });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal(DeviceConstants.MaxCount, await this.CountDevicesAsync());
        }

        [Fact]
        public async Task Get_SortAndPageParameters_ReturnsRequestedPage()
        {
            await this.AddDeviceAsync("A1", room: 10);
            await this.AddDeviceAsync("A2", room: 30);
            await this.AddDeviceAsync("A3", room: 20);

            var response = await this._client.GetAsync(
                "/api/devices?page=1&pageSize=2&sortBy=room&sortOrder=descending",
                IntegrationTest.CancellationToken
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var page = await DevicesApiTests.ReadJsonAsync(response);
            Assert.Equal(3, page.GetProperty("totalCount").GetInt32());
            Assert.Equal(2, page.GetProperty("totalPages").GetInt32());
            Assert.Equal(
                [30, 20],
                page.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("room").GetInt32())
            );
        }

        [Theory]
        [InlineData("inventoryNumber")]
        [InlineData("typeName")]
        [InlineData("name")]
        [InlineData("room")]
        public async Task Get_EachSortField_Returns200(string sortBy)
        {
            await this.AddDeviceAsync("A1");

            var response = await this._client.GetAsync($"/api/devices?sortBy={sortBy}", IntegrationTest.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Theory]
        [InlineData("sortBy=createdAt")]
        [InlineData("sortOrder=random")]
        [InlineData("page=0")]
        [InlineData("pageSize=101")]
        public async Task Get_InvalidQuery_Returns400(string query)
        {
            var response = await this._client.GetAsync($"/api/devices?{query}", IntegrationTest.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetById_MissingDevice_Returns404DeviceNotFound()
        {
            var response = await this._client.GetAsync($"/api/devices/{Guid.NewGuid()}", IntegrationTest.CancellationToken);

            await DevicesApiTests.AssertProblemAsync(response, HttpStatusCode.NotFound, "Device.NotFound");
        }

        [Fact]
        public async Task GetById_NotAGuid_Returns404()
        {
            var response = await this._client.GetAsync("/api/devices/PC1", IntegrationTest.CancellationToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Put_ExistingDevice_Returns200AndPersistsChanges()
        {
            var device = await this.AddDeviceAsync("PC1", name: "Old", room: 1);
            var types = await this.GetDeviceTypesAsync();
            var newType = types.First(type => type.Id != device.TypeId);

            var response = await this.PutAsync(
                device.Id,
                new { inventoryNumber = "PC2", name = "New", typeId = newType.Id, room = 999 }
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var updated = await DevicesApiTests.ReadJsonAsync(
                await this._client.GetAsync($"/api/devices/{device.Id}", IntegrationTest.CancellationToken)
            );
            Assert.Equal("PC2", updated.GetProperty("inventoryNumber").GetString());
            Assert.Equal("New", updated.GetProperty("name").GetString());
            Assert.Equal(newType.Name, updated.GetProperty("typeName").GetString());
            Assert.Equal(999, updated.GetProperty("room").GetInt32());
            Assert.True(updated.GetProperty("updatedAt").GetDateTime() >= updated.GetProperty("createdAt").GetDateTime());
        }

        [Fact]
        public async Task Put_KeepOwnInventoryNumber_Returns200()
        {
            var device = await this.AddDeviceAsync("PC1");

            var response = await this.PutAsync(device.Id, new { inventoryNumber = "PC1", name = "Renamed", typeId = device.TypeId, room = 2 });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Put_InventoryNumberOfAnotherDevice_Returns409()
        {
            await this.AddDeviceAsync("PC1");
            var device = await this.AddDeviceAsync("PC2");

            var response = await this.PutAsync(device.Id, new { inventoryNumber = "PC1", name = "Name", typeId = device.TypeId, room = 1 });

            await DevicesApiTests.AssertProblemAsync(response, HttpStatusCode.Conflict, "Device.AlreadyExists");
        }

        [Fact]
        public async Task Put_RegistryFull_StillAllowsEditing()
        {
            await this.AddDevicesAsync(DeviceConstants.MaxCount - 1);
            var device = await this.AddDeviceAsync("PC1");

            var response = await this.PutAsync(device.Id, new { inventoryNumber = "PC1", name = "Edited", typeId = device.TypeId, room = 1 });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Put_MissingDevice_Returns404()
        {
            var typeId = await this.GetTypeIdAsync();

            var response = await this.PutAsync(Guid.NewGuid(), new { inventoryNumber = "PC1", name = "Name", typeId, room = 1 });

            await DevicesApiTests.AssertProblemAsync(response, HttpStatusCode.NotFound, "Device.NotFound");
        }

        [Fact]
        public async Task Put_InvalidCard_Returns400AndKeepsDevice()
        {
            var device = await this.AddDeviceAsync("PC1", room: 5);

            var response = await this.PutAsync(device.Id, new { inventoryNumber = "PC1", name = "Name", typeId = device.TypeId, room = 5000 });

            await DevicesApiTests.AssertValidationErrorAsync(response, "Room");
        }

        [Fact]
        public async Task Delete_ExistingDevice_Returns204AndRemovesIt()
        {
            var device = await this.AddDeviceAsync("PC1");

            var response = await this._client.DeleteAsync($"/api/devices/{device.Id}", IntegrationTest.CancellationToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(0, await this.CountDevicesAsync());
        }

        [Fact]
        public async Task Delete_MissingDevice_Returns404()
        {
            var response = await this._client.DeleteAsync($"/api/devices/{Guid.NewGuid()}", IntegrationTest.CancellationToken);

            await DevicesApiTests.AssertProblemAsync(response, HttpStatusCode.NotFound, "Device.NotFound");
        }

        [Fact]
        public async Task Delete_RegistryFull_FreesASlot()
        {
            await this.AddDevicesAsync(DeviceConstants.MaxCount - 1);
            var device = await this.AddDeviceAsync("PC1");
            var typeId = await this.GetTypeIdAsync();

            await this._client.DeleteAsync($"/api/devices/{device.Id}", IntegrationTest.CancellationToken);
            var response = await this.PostAsync(new { inventoryNumber = "PC2", name = "Name", typeId, room = 1 });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        private async Task<Guid> GetTypeIdAsync()
            => (await this.GetDeviceTypesAsync())[0].Id;

        private Task<HttpResponseMessage> PostAsync(object body)
            => this._client.PostAsJsonAsync("/api/devices", body, IntegrationTest.CancellationToken);

        private Task<HttpResponseMessage> PutAsync(Guid deviceId, object body)
            => this._client.PutAsJsonAsync($"/api/devices/{deviceId}", body, IntegrationTest.CancellationToken);

        private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
            => await response.Content.ReadFromJsonAsync<JsonElement>(IntegrationTest.CancellationToken);

        private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status, string code)
        {
            Assert.Equal(status, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            var problem = await DevicesApiTests.ReadJsonAsync(response);
            Assert.Equal(code, problem.GetProperty("code").GetString());
            Assert.Equal((int)status, problem.GetProperty("status").GetInt32());
        }

        private static async Task<JsonElement> AssertValidationErrorAsync(HttpResponseMessage response, string field)
        {
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var problem = await DevicesApiTests.ReadJsonAsync(response);
            var errors = problem.GetProperty("errors");
            Assert.True(errors.TryGetProperty(field, out _), $"Expected a validation error for '{field}': {errors}");
            return errors;
        }
    }
}
