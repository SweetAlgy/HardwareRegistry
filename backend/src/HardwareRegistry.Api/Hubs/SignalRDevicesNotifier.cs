using HardwareRegistry.Application.Devices.Abstractions;
using HardwareRegistry.Application.Devices.Notifications;
using Microsoft.AspNetCore.SignalR;

namespace HardwareRegistry.Api.Hubs
{
    public sealed class SignalRDevicesNotifier(
        IHubContext<DevicesHub, IDevicesHubClient> hubContext,
        ILogger<SignalRDevicesNotifier> logger
    ) : IDevicesNotifier
    {
        private readonly IHubContext<DevicesHub, IDevicesHubClient> _hubContext = hubContext;
        private readonly ILogger<SignalRDevicesNotifier> _logger = logger;

        public async Task NotifyChangedAsync(DeviceChangedNotification notification)
        {
            try
            {
                await this._hubContext.Clients.All.DeviceChanged(notification);
            }
            catch (Exception exception)
            {
                // The change is already committed; clients will catch up on the next reload.
                this._logger.LogWarning(
                    exception,
                    "Failed to notify clients about device '{DeviceId}' change ({Kind}).",
                    notification.DeviceId,
                    notification.Kind
                );
            }
        }
    }
}
