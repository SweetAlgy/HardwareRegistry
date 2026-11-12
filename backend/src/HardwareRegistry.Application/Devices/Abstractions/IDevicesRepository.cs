using HardwareRegistry.Application.Common;
using HardwareRegistry.Application.Devices.Requests;
using HardwareRegistry.Domain.Devices;

namespace HardwareRegistry.Application.Devices.Abstractions
{
    public interface IDevicesRepository
    {
        public void Add(DeviceEntity deviceEntity);

        public Task<DeviceEntity?> GetTrackedByIdAsync(Guid deviceId, CancellationToken cancellationToken = default);
        public Task<DeviceEntity?> GetByIdAsync(Guid deviceId, CancellationToken cancellationToken = default);

        public Task<PagedResult<DeviceEntity>> GetPageAsync(
            GetDevicesRequest request,
            CancellationToken cancellationToken = default
        );

        public Task<int> CountAsync(CancellationToken cancellationToken = default);

        public Task<bool> DeleteByIdAsync(Guid deviceId, CancellationToken cancellationToken = default);
    }
}
