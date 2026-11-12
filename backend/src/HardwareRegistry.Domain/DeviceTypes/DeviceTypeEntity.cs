using HardwareRegistry.Domain.Common;

namespace HardwareRegistry.Domain.DeviceTypes
{
    public sealed class DeviceTypeEntity : Entity
    {
        public required string Name { get; set; }
    }
}
