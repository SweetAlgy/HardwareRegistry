using System.ComponentModel.DataAnnotations;
using HardwareRegistry.Domain.Devices;

namespace HardwareRegistry.Application.Devices.Requests
{
    /// <summary>The device card. Creating and updating a device take the same set of fields.</summary>
    public sealed record SaveDeviceRequest
    {
        [Required, StringLength(DeviceConstants.InventoryNumberMaxLength)]
        [RegularExpression(
            DeviceConstants.InventoryNumberPattern,
            ErrorMessage = "The inventory number may contain only digits and Latin letters."
        )]
        public required string InventoryNumber { get; init; }

        [Required, StringLength(DeviceConstants.NameMaxLength)]
        public required string Name { get; init; }

        [Required]
        public required Guid? TypeId { get; init; }

        [Required, Range(DeviceConstants.RoomMin, DeviceConstants.RoomMax)]
        public required int? Room { get; init; }
    }
}
