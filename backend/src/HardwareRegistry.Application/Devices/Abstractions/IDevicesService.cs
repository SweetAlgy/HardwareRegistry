using HardwareRegistry.Application.Common;
using HardwareRegistry.Application.Devices.Requests;
using HardwareRegistry.Domain.Devices;

namespace HardwareRegistry.Application.Devices.Abstractions
{
    public interface IDevicesService
    {
        public Task<PagedResult<DeviceEntity>> GetDevicesAsync(
            GetDevicesRequest request,
            CancellationToken cancellationToken = default
        );

        public Task<Result<DeviceEntity>> GetDeviceByIdAsync(
            Guid deviceId,
            CancellationToken cancellationToken = default
        );

        public Task<Result<DeviceEntity>> CreateDeviceAsync(
            SaveDeviceRequest request,
            CancellationToken cancellationToken = default
        );

        public Task<Result<DeviceEntity>> UpdateDeviceAsync(
            Guid deviceId,
            SaveDeviceRequest request,
            CancellationToken cancellationToken = default
        );

        public Task<Result> DeleteDeviceAsync(Guid deviceId, CancellationToken cancellationToken = default);
    }
}
