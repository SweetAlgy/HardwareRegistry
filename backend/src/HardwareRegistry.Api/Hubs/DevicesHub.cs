using Microsoft.AspNetCore.SignalR;

namespace HardwareRegistry.Api.Hubs
{
    /// <summary>
    /// Server-to-client only: clients subscribe to <see cref="IDevicesHubClient.DeviceChanged"/>.
    /// </summary>
    public sealed class DevicesHub : Hub<IDevicesHubClient>
    {
        public const string Route = "/hubs/devices";
    }
}
