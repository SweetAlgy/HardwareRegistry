using HardwareRegistry.Application.Common.Errors;
using HardwareRegistry.Domain.Devices;

namespace HardwareRegistry.Application.UnitTests.Common.Errors
{
    /// <summary>
    /// The error codes are part of the API contract: clients branch on them.
    /// </summary>
    public sealed class EntityErrorsTests
    {
        [Fact]
        public void NotFoundById_DeviceId_ReturnsDeviceNotFound()
        {
            var deviceId = Guid.NewGuid();

            var error = EntityErrors<DeviceEntity>.NotFoundById(deviceId);

            Assert.Equal("Device.NotFound", error.Code);
            Assert.Equal(ErrorType.NotFound, error.Type);
            Assert.Contains(deviceId.ToString(), error.Message);
            Assert.NotNull(error.Details);
            Assert.Equal(deviceId, error.Details["Id"]);
        }

        [Fact]
        public void AlreadyExists_InventoryNumber_ReturnsDeviceAlreadyExists()
        {
            var error = EntityErrors<DeviceEntity>.AlreadyExists(nameof(DeviceEntity.InventoryNumber), "PC1");

            Assert.Equal("Device.AlreadyExists", error.Code);
            Assert.Equal(ErrorType.Conflict, error.Type);
            Assert.Equal("Device with inventory number: 'PC1' already exists.", error.Message);
            Assert.NotNull(error.Details);
            Assert.Equal("PC1", error.Details[nameof(DeviceEntity.InventoryNumber)]);
        }

        [Fact]
        public void Invalid_Message_ReturnsValidationError()
        {
            var error = EntityErrors<DeviceEntity>.Invalid("Bad device.");

            Assert.Equal("Device.Invalid", error.Code);
            Assert.Equal(ErrorType.Validation, error.Type);
            Assert.Equal("Bad device.", error.Message);
            Assert.Null(error.Details);
        }

        [Fact]
        public void NotFound_NoMessage_UsesHumanizedEntityName()
        {
            var error = EntityErrors<DeviceEntity>.NotFound();

            Assert.Equal("Device was not found.", error.Message);
        }

        [Fact]
        public void NoneFoundByIds_Ids_ListsEveryId()
        {
            Guid[] ids = [Guid.NewGuid(), Guid.NewGuid()];

            var error = EntityErrors<DeviceEntity>.NoneFoundByIds(ids);

            Assert.Equal("Device.NoneFound", error.Code);
            Assert.All(ids, id => Assert.Contains(id.ToString(), error.Message));
        }
    }
}
