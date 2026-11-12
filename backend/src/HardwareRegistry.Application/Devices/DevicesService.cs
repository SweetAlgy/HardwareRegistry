using HardwareRegistry.Application.Abstractions.Persistence;
using HardwareRegistry.Application.Common;
using HardwareRegistry.Application.Common.Errors;
using HardwareRegistry.Application.Common.Exceptions;
using HardwareRegistry.Application.Devices.Abstractions;
using HardwareRegistry.Application.Devices.Notifications;
using HardwareRegistry.Application.Devices.Requests;
using HardwareRegistry.Application.DeviceTypes.Abstractions;
using HardwareRegistry.Domain.Devices;
using Microsoft.Extensions.Logging;

namespace HardwareRegistry.Application.Devices
{
    public sealed class DevicesService(
        ILoggerFactory loggerFactory,
        TimeProvider timeProvider,
        IUnitOfWork unitOfWork,
        IDevicesRepository devicesRepository,
        IDeviceTypesRepository deviceTypesRepository,
        IDeviceRegistryLock deviceRegistryLock,
        IDevicesNotifier devicesNotifier
    ) : ServiceBase(loggerFactory), IDevicesService
    {
        private readonly TimeProvider _timeProvider = timeProvider;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IDevicesRepository _devicesRepository = devicesRepository;
        private readonly IDeviceTypesRepository _deviceTypesRepository = deviceTypesRepository;
        private readonly IDeviceRegistryLock _deviceRegistryLock = deviceRegistryLock;
        private readonly IDevicesNotifier _devicesNotifier = devicesNotifier;

        public async Task<PagedResult<DeviceEntity>> GetDevicesAsync(
            GetDevicesRequest request,
            CancellationToken cancellationToken = default
        ) => await this._devicesRepository.GetPageAsync(request, cancellationToken);

        public async Task<Result<DeviceEntity>> GetDeviceByIdAsync(
            Guid deviceId,
            CancellationToken cancellationToken = default
        )
        {
            var deviceEntity = await this._devicesRepository.GetByIdAsync(deviceId, cancellationToken);

            if (deviceEntity is null)
            {
                this.Logger.LogWarning("Device with id: '{DeviceId}' not found.", deviceId);
                return EntityErrors<DeviceEntity>.NotFoundById(deviceId);
            }

            return deviceEntity;
        }

        public async Task<Result<DeviceEntity>> CreateDeviceAsync(
            SaveDeviceRequest request,
            CancellationToken cancellationToken = default
        )
        {
            var typeId = request.TypeId!.Value;
            var deviceTypeEntity = await this._deviceTypesRepository.GetTrackedByIdAsync(typeId, cancellationToken);

            if (deviceTypeEntity is null)
                return DevicesService.UnknownDeviceType(typeId);

            await using var transaction = await this._unitOfWork.BeginTransactionAsync(cancellationToken);

            // Without the lock two concurrent requests could both see MaxCount - 1 devices
            // and both insert.
            await this._deviceRegistryLock.AcquireAsync(cancellationToken);

            var devicesCount = await this._devicesRepository.CountAsync(cancellationToken);

            if (devicesCount >= DeviceConstants.MaxCount)
            {
                this.Logger.LogWarning(
                    "Device creation rejected: the registry already holds {DevicesCount} devices.",
                    devicesCount
                );

                return OperationErrors.LimitExceeded("devices", DeviceConstants.MaxCount);
            }

            var now = this._timeProvider.GetUtcNow().UtcDateTime;

            var newDeviceEntity = new DeviceEntity(now)
            {
                InventoryNumber = request.InventoryNumber,
                Name = request.Name.Trim(),
                TypeId = deviceTypeEntity.Id,
                Type = deviceTypeEntity,
                Room = request.Room!.Value
            };

            this._devicesRepository.Add(newDeviceEntity);

            try
            {
                await this._unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (PersistenceConflictException exception)
            {
                this.Logger.LogWarning(
                    exception,
                    "Device creation conflict for inventory number '{InventoryNumber}'.",
                    request.InventoryNumber
                );

                return EntityErrors<DeviceEntity>.AlreadyExists(
                    nameof(DeviceEntity.InventoryNumber),
                    request.InventoryNumber
                );
            }

            await transaction.CommitAsync(cancellationToken);

            this.Logger.LogInformation("Device '{DeviceId}' created.", newDeviceEntity.Id);

            await this._devicesNotifier.NotifyChangedAsync(new(newDeviceEntity.Id, DeviceChangeKind.Created));

            return newDeviceEntity;
        }

        public async Task<Result<DeviceEntity>> UpdateDeviceAsync(
            Guid deviceId,
            SaveDeviceRequest request,
            CancellationToken cancellationToken = default
        )
        {
            var deviceEntity = await this._devicesRepository.GetTrackedByIdAsync(deviceId, cancellationToken);

            if (deviceEntity is null)
            {
                this.Logger.LogWarning("Device with id: '{DeviceId}' not found.", deviceId);
                return EntityErrors<DeviceEntity>.NotFoundById(deviceId);
            }

            var typeId = request.TypeId!.Value;
            var deviceTypeEntity = await this._deviceTypesRepository.GetTrackedByIdAsync(typeId, cancellationToken);

            if (deviceTypeEntity is null)
                return DevicesService.UnknownDeviceType(typeId);

            deviceEntity.InventoryNumber = request.InventoryNumber;
            deviceEntity.Name = request.Name.Trim();
            deviceEntity.TypeId = deviceTypeEntity.Id;
            deviceEntity.Type = deviceTypeEntity;
            deviceEntity.Room = request.Room!.Value;
            deviceEntity.MarkUpdated(this._timeProvider.GetUtcNow().UtcDateTime);

            try
            {
                await this._unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (PersistenceConflictException exception)
            {
                this.Logger.LogWarning(
                    exception,
                    "Device '{DeviceId}' update conflict for inventory number '{InventoryNumber}'.",
                    deviceId,
                    request.InventoryNumber
                );

                return EntityErrors<DeviceEntity>.AlreadyExists(
                    nameof(DeviceEntity.InventoryNumber),
                    request.InventoryNumber
                );
            }

            this.Logger.LogInformation("Device '{DeviceId}' updated.", deviceId);

            await this._devicesNotifier.NotifyChangedAsync(new(deviceId, DeviceChangeKind.Updated));

            return deviceEntity;
        }

        public async Task<Result> DeleteDeviceAsync(Guid deviceId, CancellationToken cancellationToken = default)
        {
            if (!await this._devicesRepository.DeleteByIdAsync(deviceId, cancellationToken))
            {
                this.Logger.LogWarning("Device with id: '{DeviceId}' not found.", deviceId);
                return EntityErrors<DeviceEntity>.NotFoundById(deviceId);
            }

            this.Logger.LogInformation("Device '{DeviceId}' deleted.", deviceId);

            await this._devicesNotifier.NotifyChangedAsync(new(deviceId, DeviceChangeKind.Deleted));

            return Result.Success();
        }

        private static Error UnknownDeviceType(Guid typeId)
            => EntityErrors<DeviceEntity>.Invalid(
                $"Device type with id: '{typeId}' does not exist.",
                new Dictionary<string, object?>
                {
                    [nameof(DeviceEntity.TypeId)] = typeId
                }
            );
    }
}
