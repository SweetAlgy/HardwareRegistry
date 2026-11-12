namespace HardwareRegistry.Application.Devices.Abstractions
{
    /// <summary>
    /// Serializes the operations that depend on the total number of devices.
    /// Held until the surrounding transaction ends.
    /// </summary>
    public interface IDeviceRegistryLock
    {
        public Task AcquireAsync(CancellationToken cancellationToken = default);
    }
}
