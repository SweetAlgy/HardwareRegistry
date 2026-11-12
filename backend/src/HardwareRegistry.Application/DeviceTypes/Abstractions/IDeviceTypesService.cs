using HardwareRegistry.Domain.DeviceTypes;

namespace HardwareRegistry.Application.DeviceTypes.Abstractions
{
    public interface IDeviceTypesService
    {
        public Task<IReadOnlyList<DeviceTypeEntity>> GetDeviceTypesAsync(
            CancellationToken cancellationToken = default
        );
    }
}
