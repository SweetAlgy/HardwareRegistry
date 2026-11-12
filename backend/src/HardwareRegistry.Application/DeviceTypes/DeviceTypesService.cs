using HardwareRegistry.Application.DeviceTypes.Abstractions;
using HardwareRegistry.Domain.DeviceTypes;

namespace HardwareRegistry.Application.DeviceTypes
{
    public sealed class DeviceTypesService(IDeviceTypesRepository deviceTypesRepository) : IDeviceTypesService
    {
        private readonly IDeviceTypesRepository _deviceTypesRepository = deviceTypesRepository;

        public async Task<IReadOnlyList<DeviceTypeEntity>> GetDeviceTypesAsync(
            CancellationToken cancellationToken = default
        ) => await this._deviceTypesRepository.GetAllAsync(cancellationToken);
    }
}
