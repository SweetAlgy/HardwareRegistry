using HardwareRegistry.Application.Devices.Notifications;

namespace HardwareRegistry.Application.Devices.Abstractions
{
    /// <summary>
    /// Tells connected clients that the registry has changed. Called after the change is committed
    /// and must not fail the operation that triggered it.
    /// </summary>
    public interface IDevicesNotifier
    {
        public Task NotifyChangedAsync(DeviceChangedNotification notification);
    }
}
