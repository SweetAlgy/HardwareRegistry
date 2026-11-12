using HardwareRegistry.Api.Hubs;
using HardwareRegistry.Application.Devices.Notifications;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace HardwareRegistry.Api.UnitTests.Hubs
{
    public sealed class SignalRDevicesNotifierTests
    {
        private readonly IHubContext<DevicesHub, IDevicesHubClient> _hubContext =
            Substitute.For<IHubContext<DevicesHub, IDevicesHubClient>>();

        private readonly IDevicesHubClient _allClients = Substitute.For<IDevicesHubClient>();

        private readonly FakeLogger<SignalRDevicesNotifier> _logger = new();

        public SignalRDevicesNotifierTests()
            => this._hubContext.Clients.All.Returns(this._allClients);

        [Fact]
        public async Task NotifyChangedAsync_Notification_SendsItToAllClients()
        {
            var notification = new DeviceChangedNotification(Guid.NewGuid(), DeviceChangeKind.Created);

            await new SignalRDevicesNotifier(this._hubContext, this._logger).NotifyChangedAsync(notification);

            await this._allClients.Received(1).DeviceChanged(notification);
        }

        [Fact]
        public async Task NotifyChangedAsync_SendFails_LogsWarningInsteadOfThrowing()
        {
            var notification = new DeviceChangedNotification(Guid.NewGuid(), DeviceChangeKind.Deleted);
            var failure = new IOException("Backplane is down.");
            this._allClients.DeviceChanged(notification).ThrowsAsync(failure);

            await new SignalRDevicesNotifier(this._hubContext, this._logger).NotifyChangedAsync(notification);

            var record = Assert.Single(this._logger.Collector.GetSnapshot());
            Assert.Equal(LogLevel.Warning, record.Level);
            Assert.Same(failure, record.Exception);
        }
    }
}
