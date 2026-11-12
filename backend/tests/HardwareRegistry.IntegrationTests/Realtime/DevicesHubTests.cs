using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Channels;
using HardwareRegistry.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace HardwareRegistry.IntegrationTests.Realtime
{
    /// <summary>
    /// Checks the wire format the browser clients rely on: a "DeviceChanged" message
    /// with a camelCase payload and the change kind as a string.
    /// </summary>
    public sealed class DevicesHubTests(TestEnvironment environment) : IntegrationTest(environment)
    {
        private static readonly TimeSpan MessageTimeout = TimeSpan.FromSeconds(10);

        private readonly Channel<JsonElement> _messages = Channel.CreateUnbounded<JsonElement>();
        private HubConnection? _connection;

        private HubConnection Connection => this._connection!;

        public override async ValueTask InitializeAsync()
        {
            await base.InitializeAsync();

            var server = this.Environment.Factory.Server;

            this._connection = new HubConnectionBuilder()
                .WithUrl(
                    new Uri(server.BaseAddress, "/hubs/devices"),
                    options =>
                    {
                        // The in-memory test server only speaks HTTP, so the transport must be long polling.
                        options.Transports = HttpTransportType.LongPolling;
                        options.HttpMessageHandlerFactory = _ => server.CreateHandler();
                    }
                )
                .Build();

            this._connection.On<JsonElement>("DeviceChanged", message => this._messages.Writer.TryWrite(message));

            await this._connection.StartAsync(IntegrationTest.CancellationToken);
        }

        public override async ValueTask DisposeAsync()
        {
            if (this._connection is not null)
                await this._connection.DisposeAsync();

            await base.DisposeAsync();
        }

        [Fact]
        public async Task Post_DeviceCreated_BroadcastsCreated()
        {
            var typeId = (await this.GetDeviceTypesAsync())[0].Id;
            var client = this.Environment.Factory.CreateClient();

            var response = await client.PostAsJsonAsync(
                "/api/devices",
                new { inventoryNumber = "PC1", name = "Name", typeId, room = 1 },
                IntegrationTest.CancellationToken
            );
            var device = await response.Content.ReadFromJsonAsync<JsonElement>(IntegrationTest.CancellationToken);

            var message = await this.ReceiveAsync();
            Assert.Equal(device.GetProperty("id").GetGuid(), message.GetProperty("deviceId").GetGuid());
            Assert.Equal("Created", message.GetProperty("kind").GetString());
        }

        [Fact]
        public async Task Put_DeviceUpdated_BroadcastsUpdated()
        {
            var device = await this.AddDeviceAsync("PC1");
            var client = this.Environment.Factory.CreateClient();

            await client.PutAsJsonAsync(
                $"/api/devices/{device.Id}",
                new { inventoryNumber = "PC1", name = "Renamed", typeId = device.TypeId, room = 1 },
                IntegrationTest.CancellationToken
            );

            var message = await this.ReceiveAsync();
            Assert.Equal(device.Id, message.GetProperty("deviceId").GetGuid());
            Assert.Equal("Updated", message.GetProperty("kind").GetString());
        }

        [Fact]
        public async Task Delete_DeviceDeleted_BroadcastsDeleted()
        {
            var device = await this.AddDeviceAsync("PC1");
            var client = this.Environment.Factory.CreateClient();

            await client.DeleteAsync($"/api/devices/{device.Id}", IntegrationTest.CancellationToken);

            var message = await this.ReceiveAsync();
            Assert.Equal(device.Id, message.GetProperty("deviceId").GetGuid());
            Assert.Equal("Deleted", message.GetProperty("kind").GetString());
        }

        [Fact]
        public async Task Post_RejectedChange_BroadcastsNothing()
        {
            await this.AddDeviceAsync("PC1");
            var typeId = (await this.GetDeviceTypesAsync())[0].Id;
            var client = this.Environment.Factory.CreateClient();

            await client.PostAsJsonAsync(
                "/api/devices",
                new { inventoryNumber = "PC1", name = "Duplicate", typeId, room = 1 },
                IntegrationTest.CancellationToken
            );

            await Task.Delay(TimeSpan.FromSeconds(1), IntegrationTest.CancellationToken);
            Assert.False(this._messages.Reader.TryRead(out _));
        }

        [Fact]
        public async Task Hub_SecondClient_ReceivesTheSameChange()
        {
            var server = this.Environment.Factory.Server;
            var secondMessages = Channel.CreateUnbounded<JsonElement>();
            await using var secondConnection = new HubConnectionBuilder()
                .WithUrl(
                    new Uri(server.BaseAddress, "/hubs/devices"),
                    options =>
                    {
                        options.Transports = HttpTransportType.LongPolling;
                        options.HttpMessageHandlerFactory = _ => server.CreateHandler();
                    }
                )
                .Build();
            secondConnection.On<JsonElement>("DeviceChanged", message => secondMessages.Writer.TryWrite(message));
            await secondConnection.StartAsync(IntegrationTest.CancellationToken);
            var device = await this.AddDeviceAsync("PC1");

            await this.Environment.Factory.CreateClient()
                .DeleteAsync($"/api/devices/{device.Id}", IntegrationTest.CancellationToken);

            var first = await this.ReceiveAsync();
            var second = await secondMessages.Reader
                .ReadAsync(IntegrationTest.CancellationToken)
                .AsTask()
                .WaitAsync(DevicesHubTests.MessageTimeout, IntegrationTest.CancellationToken);
            Assert.Equal(first.GetProperty("deviceId").GetGuid(), second.GetProperty("deviceId").GetGuid());
        }

        private async Task<JsonElement> ReceiveAsync()
            => await this._messages.Reader
                .ReadAsync(IntegrationTest.CancellationToken)
                .AsTask()
                .WaitAsync(DevicesHubTests.MessageTimeout, IntegrationTest.CancellationToken);
    }
}
