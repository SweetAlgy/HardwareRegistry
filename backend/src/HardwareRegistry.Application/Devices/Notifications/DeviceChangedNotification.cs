namespace HardwareRegistry.Application.Devices.Notifications
{
    public sealed record DeviceChangedNotification(Guid DeviceId, DeviceChangeKind Kind);
}
