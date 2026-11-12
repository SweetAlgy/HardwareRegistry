using HardwareRegistry.Domain.DeviceTypes;

namespace HardwareRegistry.Application.DeviceTypes.Responses
{
    public sealed record DeviceTypeResponse
    {
        private DeviceTypeResponse(DeviceTypeEntity entity)
        {
            this.Id = entity.Id;
            this.Name = entity.Name;
        }

        public Guid Id { get; }
        public string Name { get; }

        public static DeviceTypeResponse FromEntity(DeviceTypeEntity entity)
            => new(entity);
    }
}
