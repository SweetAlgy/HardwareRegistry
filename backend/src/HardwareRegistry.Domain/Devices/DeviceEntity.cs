using HardwareRegistry.Domain.Common;
using HardwareRegistry.Domain.DeviceTypes;

namespace HardwareRegistry.Domain.Devices
{
    public sealed class DeviceEntity : TimestampedEntity
    {
        public DeviceEntity(Guid id, DateTime createdAt, DateTime updatedAt) : base(id, createdAt, updatedAt) { }
        public DeviceEntity(DateTime createdAt) : base(createdAt) { }

        public required string InventoryNumber { get; set; }
        public required string Name { get; set; }

        public required Guid TypeId { get; set; }
        public DeviceTypeEntity? Type { get; set; }

        public required int Room { get; set; }
    }
}
