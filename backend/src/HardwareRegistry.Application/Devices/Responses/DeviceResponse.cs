using HardwareRegistry.Domain.Devices;

namespace HardwareRegistry.Application.Devices.Responses
{
    public sealed record DeviceResponse
    {
        private DeviceResponse(DeviceEntity entity)
        {
            this.Id = entity.Id;
            this.CreatedAt = entity.CreatedAt;
            this.UpdatedAt = entity.UpdatedAt;
            this.InventoryNumber = entity.InventoryNumber;
            this.Name = entity.Name;
            this.TypeId = entity.TypeId;
            this.TypeName = entity.Type?.Name ?? String.Empty;
            this.Room = entity.Room;
        }

        public Guid Id { get; }

        public DateTime CreatedAt { get; }
        public DateTime UpdatedAt { get; }

        public string InventoryNumber { get; }
        public string Name { get; }

        public Guid TypeId { get; }
        public string TypeName { get; }

        public int Room { get; }

        public static DeviceResponse FromEntity(DeviceEntity entity)
            => new(entity);
    }
}
