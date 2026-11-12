using HardwareRegistry.Application.Devices.Notifications;

namespace HardwareRegistry.Api.Hubs
{
    public interface IDevicesHubClient
    {
        public Task DeviceChanged(DeviceChangedNotification notification);
    }
}
