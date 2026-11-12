using HardwareRegistry.Application.Abstractions.Persistence;
using HardwareRegistry.Application.Common;
using HardwareRegistry.Application.Common.Exceptions;
using HardwareRegistry.Application.Devices;
using HardwareRegistry.Application.Devices.Abstractions;
using HardwareRegistry.Application.Devices.Notifications;
using HardwareRegistry.Application.Devices.Requests;
using HardwareRegistry.Application.DeviceTypes.Abstractions;
using HardwareRegistry.Domain.Devices;
using HardwareRegistry.Domain.DeviceTypes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace HardwareRegistry.Application.UnitTests.Devices
{
    public sealed class DevicesServiceTests
    {
        private static readonly DateTimeOffset Now = new(2026, 11, 12, 9, 30, 0, TimeSpan.Zero);

        private readonly FakeTimeProvider _timeProvider = new(DevicesServiceTests.Now);
        private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
        private readonly ITransaction _transaction = Substitute.For<ITransaction>();
        private readonly IDevicesRepository _devicesRepository = Substitute.For<IDevicesRepository>();
        private readonly IDeviceTypesRepository _deviceTypesRepository = Substitute.For<IDeviceTypesRepository>();
        private readonly IDeviceRegistryLock _deviceRegistryLock = Substitute.For<IDeviceRegistryLock>();
        private readonly IDevicesNotifier _devicesNotifier = Substitute.For<IDevicesNotifier>();

        private readonly DeviceTypeEntity _monitorType = new() { Name = "Монитор" };

        private readonly DevicesService _service;

        public DevicesServiceTests()
        {
            this._unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>()).Returns(this._transaction);

            this._deviceTypesRepository
                .GetTrackedByIdAsync(this._monitorType.Id, Arg.Any<CancellationToken>())
                .Returns(this._monitorType);

            this._service = new(
                NullLoggerFactory.Instance,
                this._timeProvider,
                this._unitOfWork,
                this._devicesRepository,
                this._deviceTypesRepository,
                this._deviceRegistryLock,
                this._devicesNotifier
            );
        }

        private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

        [Fact]
        public async Task GetDevicesAsync_Request_ReturnsRepositoryPage()
        {
            var request = new GetDevicesRequest { Page = 2, PageSize = 20, SortBy = DeviceSortField.Room };
            var page = new PagedResult<DeviceEntity> { Page = 2, PageSize = 20, TotalCount = 21 };
            this._devicesRepository.GetPageAsync(request, Arg.Any<CancellationToken>()).Returns(page);

            var result = await this._service.GetDevicesAsync(request, DevicesServiceTests.CancellationToken);

            Assert.Same(page, result);
        }

        [Fact]
        public async Task GetDeviceByIdAsync_ExistingDevice_ReturnsDevice()
        {
            var device = this.CreateDevice();
            this._devicesRepository.GetByIdAsync(device.Id, Arg.Any<CancellationToken>()).Returns(device);

            var result = await this._service.GetDeviceByIdAsync(device.Id, DevicesServiceTests.CancellationToken);

            Assert.True(result.IsSuccess);
            Assert.Same(device, result.Value);
        }

        [Fact]
        public async Task GetDeviceByIdAsync_MissingDevice_ReturnsNotFound()
        {
            var result = await this._service.GetDeviceByIdAsync(Guid.NewGuid(), DevicesServiceTests.CancellationToken);

            Assert.True(result.IsFailure);
            Assert.Equal("Device.NotFound", result.Error.Code);
        }

        [Fact]
        public async Task CreateDeviceAsync_ValidRequest_AddsDeviceFromCard()
        {
            DeviceEntity? addedDevice = null;
            this._devicesRepository.Add(Arg.Do<DeviceEntity>(device => addedDevice = device));

            var result = await this._service.CreateDeviceAsync(
                this.CreateRequest(name: "  Dell P2422H  "),
                DevicesServiceTests.CancellationToken
            );

            Assert.True(result.IsSuccess);
            Assert.NotNull(addedDevice);
            Assert.Same(addedDevice, result.Value);
            Assert.Equal("MON1", addedDevice.InventoryNumber);
            Assert.Equal("Dell P2422H", addedDevice.Name);
            Assert.Equal(this._monitorType.Id, addedDevice.TypeId);
            Assert.Same(this._monitorType, addedDevice.Type);
            Assert.Equal(204, addedDevice.Room);
            Assert.Equal(DevicesServiceTests.Now.UtcDateTime, addedDevice.CreatedAt);
            Assert.Equal(DevicesServiceTests.Now.UtcDateTime, addedDevice.UpdatedAt);
        }

        [Fact]
        public async Task CreateDeviceAsync_ValidRequest_CountsUnderLockAndCommits()
        {
            await this._service.CreateDeviceAsync(this.CreateRequest(), DevicesServiceTests.CancellationToken);

            Received.InOrder(() =>
                {
                    this._unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>());
                    this._deviceRegistryLock.AcquireAsync(Arg.Any<CancellationToken>());
                    this._devicesRepository.CountAsync(Arg.Any<CancellationToken>());
                    this._devicesRepository.Add(Arg.Any<DeviceEntity>());
                    this._unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>());
                    this._transaction.CommitAsync(Arg.Any<CancellationToken>());
                    this._devicesNotifier.NotifyChangedAsync(Arg.Any<DeviceChangedNotification>());
                }
            );
            await this._transaction.Received(1).DisposeAsync();
        }

        [Fact]
        public async Task CreateDeviceAsync_ValidRequest_NotifiesCreated()
        {
            var result = await this._service.CreateDeviceAsync(this.CreateRequest(), DevicesServiceTests.CancellationToken);

            await this._devicesNotifier.Received(1).NotifyChangedAsync(
                new DeviceChangedNotification(result.Value.Id, DeviceChangeKind.Created)
            );
        }

        [Fact]
        public async Task CreateDeviceAsync_OneBelowLimit_CreatesDevice()
        {
            this._devicesRepository.CountAsync(Arg.Any<CancellationToken>()).Returns(DeviceConstants.MaxCount - 1);

            var result = await this._service.CreateDeviceAsync(this.CreateRequest(), DevicesServiceTests.CancellationToken);

            Assert.True(result.IsSuccess);
        }

        [Theory]
        [InlineData(DeviceConstants.MaxCount)]
        [InlineData(DeviceConstants.MaxCount + 1)]
        public async Task CreateDeviceAsync_LimitReached_ReturnsLimitExceededWithoutAdding(int devicesCount)
        {
            this._devicesRepository.CountAsync(Arg.Any<CancellationToken>()).Returns(devicesCount);

            var result = await this._service.CreateDeviceAsync(this.CreateRequest(), DevicesServiceTests.CancellationToken);

            Assert.True(result.IsFailure);
            Assert.Equal("Operation.LimitExceeded", result.Error.Code);
            Assert.NotNull(result.Error.Details);
            Assert.Equal(DeviceConstants.MaxCount, result.Error.Details["maximum"]);
            this._devicesRepository.DidNotReceive().Add(Arg.Any<DeviceEntity>());
            await this._unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
            await this._transaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
            await this._transaction.Received(1).DisposeAsync();
            await this._devicesNotifier.DidNotReceive().NotifyChangedAsync(Arg.Any<DeviceChangedNotification>());
        }

        [Fact]
        public async Task CreateDeviceAsync_UnknownType_ReturnsInvalidWithoutOpeningTransaction()
        {
            var unknownTypeId = Guid.NewGuid();

            var result = await this._service.CreateDeviceAsync(
                this.CreateRequest() with { TypeId = unknownTypeId },
                DevicesServiceTests.CancellationToken
            );

            Assert.True(result.IsFailure);
            Assert.Equal("Device.Invalid", result.Error.Code);
            Assert.NotNull(result.Error.Details);
            Assert.Equal(unknownTypeId, result.Error.Details[nameof(DeviceEntity.TypeId)]);
            await this._unitOfWork.DidNotReceive().BeginTransactionAsync(Arg.Any<CancellationToken>());
            this._devicesRepository.DidNotReceive().Add(Arg.Any<DeviceEntity>());
        }

        [Fact]
        public async Task CreateDeviceAsync_DuplicateInventoryNumber_ReturnsAlreadyExistsWithoutCommit()
        {
            this._unitOfWork
                .SaveChangesAsync(Arg.Any<CancellationToken>())
                .ThrowsAsync(DevicesServiceTests.CreateConflict());

            var result = await this._service.CreateDeviceAsync(this.CreateRequest(), DevicesServiceTests.CancellationToken);

            Assert.True(result.IsFailure);
            Assert.Equal("Device.AlreadyExists", result.Error.Code);
            Assert.NotNull(result.Error.Details);
            Assert.Equal("MON1", result.Error.Details[nameof(DeviceEntity.InventoryNumber)]);
            await this._transaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
            await this._transaction.Received(1).DisposeAsync();
            await this._devicesNotifier.DidNotReceive().NotifyChangedAsync(Arg.Any<DeviceChangedNotification>());
        }

        [Fact]
        public async Task CreateDeviceAsync_UnexpectedPersistenceError_Propagates()
        {
            this._unitOfWork
                .SaveChangesAsync(Arg.Any<CancellationToken>())
                .ThrowsAsync(new InvalidOperationException("Connection lost."));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                this._service.CreateDeviceAsync(this.CreateRequest(), DevicesServiceTests.CancellationToken)
            );
            await this._transaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
            await this._transaction.Received(1).DisposeAsync();
        }

        [Fact]
        public async Task UpdateDeviceAsync_ExistingDevice_UpdatesCardAndUpdatedAt()
        {
            var device = this.CreateDevice();
            var printerType = new DeviceTypeEntity { Name = "Принтер" };
            this._devicesRepository.GetTrackedByIdAsync(device.Id, Arg.Any<CancellationToken>()).Returns(device);
            this._deviceTypesRepository
                .GetTrackedByIdAsync(printerType.Id, Arg.Any<CancellationToken>())
                .Returns(printerType);
            this._timeProvider.Advance(TimeSpan.FromHours(2));

            var result = await this._service.UpdateDeviceAsync(
                device.Id,
                new()
                {
                    InventoryNumber = "PRN9",
                    Name = " HP LaserJet ",
                    TypeId = printerType.Id,
                    Room = 1000
                },
                DevicesServiceTests.CancellationToken
            );

            Assert.True(result.IsSuccess);
            Assert.Same(device, result.Value);
            Assert.Equal("PRN9", device.InventoryNumber);
            Assert.Equal("HP LaserJet", device.Name);
            Assert.Equal(printerType.Id, device.TypeId);
            Assert.Same(printerType, device.Type);
            Assert.Equal(1000, device.Room);
            Assert.Equal(DevicesServiceTests.Now.UtcDateTime, device.CreatedAt);
            Assert.Equal(DevicesServiceTests.Now.AddHours(2).UtcDateTime, device.UpdatedAt);
            await this._unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdateDeviceAsync_ExistingDevice_DoesNotTakeRegistryLock()
        {
            var device = this.CreateDevice();
            this._devicesRepository.GetTrackedByIdAsync(device.Id, Arg.Any<CancellationToken>()).Returns(device);

            await this._service.UpdateDeviceAsync(device.Id, this.CreateRequest(), DevicesServiceTests.CancellationToken);

            await this._deviceRegistryLock.DidNotReceive().AcquireAsync(Arg.Any<CancellationToken>());
            await this._unitOfWork.DidNotReceive().BeginTransactionAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdateDeviceAsync_ExistingDevice_NotifiesUpdated()
        {
            var device = this.CreateDevice();
            this._devicesRepository.GetTrackedByIdAsync(device.Id, Arg.Any<CancellationToken>()).Returns(device);

            await this._service.UpdateDeviceAsync(device.Id, this.CreateRequest(), DevicesServiceTests.CancellationToken);

            await this._devicesNotifier.Received(1).NotifyChangedAsync(
                new DeviceChangedNotification(device.Id, DeviceChangeKind.Updated)
            );
        }

        [Fact]
        public async Task UpdateDeviceAsync_MissingDevice_ReturnsNotFoundWithoutSaving()
        {
            var result = await this._service.UpdateDeviceAsync(
                Guid.NewGuid(),
                this.CreateRequest(),
                DevicesServiceTests.CancellationToken
            );

            Assert.True(result.IsFailure);
            Assert.Equal("Device.NotFound", result.Error.Code);
            await this._unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
            await this._devicesNotifier.DidNotReceive().NotifyChangedAsync(Arg.Any<DeviceChangedNotification>());
        }

        [Fact]
        public async Task UpdateDeviceAsync_UnknownType_ReturnsInvalidAndKeepsDevice()
        {
            var device = this.CreateDevice();
            this._devicesRepository.GetTrackedByIdAsync(device.Id, Arg.Any<CancellationToken>()).Returns(device);

            var result = await this._service.UpdateDeviceAsync(
                device.Id,
                this.CreateRequest() with { InventoryNumber = "OTHER1", TypeId = Guid.NewGuid() },
                DevicesServiceTests.CancellationToken
            );

            Assert.True(result.IsFailure);
            Assert.Equal("Device.Invalid", result.Error.Code);
            Assert.Equal("MON1", device.InventoryNumber);
            await this._unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task UpdateDeviceAsync_DuplicateInventoryNumber_ReturnsAlreadyExistsWithoutNotifying()
        {
            var device = this.CreateDevice();
            this._devicesRepository.GetTrackedByIdAsync(device.Id, Arg.Any<CancellationToken>()).Returns(device);
            this._unitOfWork
                .SaveChangesAsync(Arg.Any<CancellationToken>())
                .ThrowsAsync(DevicesServiceTests.CreateConflict());

            var result = await this._service.UpdateDeviceAsync(
                device.Id,
                this.CreateRequest() with { InventoryNumber = "TAKEN1" },
                DevicesServiceTests.CancellationToken
            );

            Assert.True(result.IsFailure);
            Assert.Equal("Device.AlreadyExists", result.Error.Code);
            Assert.NotNull(result.Error.Details);
            Assert.Equal("TAKEN1", result.Error.Details[nameof(DeviceEntity.InventoryNumber)]);
            await this._devicesNotifier.DidNotReceive().NotifyChangedAsync(Arg.Any<DeviceChangedNotification>());
        }

        [Fact]
        public async Task DeleteDeviceAsync_ExistingDevice_ReturnsSuccessAndNotifiesDeleted()
        {
            var deviceId = Guid.NewGuid();
            this._devicesRepository.DeleteByIdAsync(deviceId, Arg.Any<CancellationToken>()).Returns(true);

            var result = await this._service.DeleteDeviceAsync(deviceId, DevicesServiceTests.CancellationToken);

            Assert.True(result.IsSuccess);
            await this._devicesNotifier.Received(1).NotifyChangedAsync(
                new DeviceChangedNotification(deviceId, DeviceChangeKind.Deleted)
            );
        }

        [Fact]
        public async Task DeleteDeviceAsync_MissingDevice_ReturnsNotFoundWithoutNotifying()
        {
            var result = await this._service.DeleteDeviceAsync(Guid.NewGuid(), DevicesServiceTests.CancellationToken);

            Assert.True(result.IsFailure);
            Assert.Equal("Device.NotFound", result.Error.Code);
            await this._devicesNotifier.DidNotReceive().NotifyChangedAsync(Arg.Any<DeviceChangedNotification>());
        }

        private SaveDeviceRequest CreateRequest(string inventoryNumber = "MON1", string name = "Dell P2422H")
            => new()
            {
                InventoryNumber = inventoryNumber,
                Name = name,
                TypeId = this._monitorType.Id,
                Room = 204
            };

        private DeviceEntity CreateDevice()
            => new(DevicesServiceTests.Now.UtcDateTime)
            {
                InventoryNumber = "MON1",
                Name = "Dell P2422H",
                TypeId = this._monitorType.Id,
                Type = this._monitorType,
                Room = 204
            };

        private static PersistenceConflictException CreateConflict()
            => new(typeof(DeviceEntity), nameof(DeviceEntity.InventoryNumber), "MON1", "ix_devices_inventory_number");
    }
}
