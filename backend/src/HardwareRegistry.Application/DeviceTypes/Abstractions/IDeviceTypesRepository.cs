using HardwareRegistry.Domain.DeviceTypes;

namespace HardwareRegistry.Application.DeviceTypes.Abstractions
{
    public interface IDeviceTypesRepository
    {
        public Task<IReadOnlyList<DeviceTypeEntity>> GetAllAsync(CancellationToken cancellationToken = default);

        public Task<DeviceTypeEntity?> GetTrackedByIdAsync(
            Guid deviceTypeId,
            CancellationToken cancellationToken = default
        );
    }
}
