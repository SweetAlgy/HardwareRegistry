namespace HardwareRegistry.Infrastructure.Persistence.Postgres
{
    public static class ConstraintNames
    {
        public const string CreatedAtMustNotBeLaterThanUpdatedAt = "ck_entity_created_at_not_later_than_updated_at";

        public const string DeviceInventoryNumberIndex = "ix_devices_inventory_number";
        public const string DeviceInventoryNumberFormat = "ck_devices_inventory_number_format";
        public const string DeviceRoomRange = "ck_devices_room_range";

        public const string DeviceTypeNameIndex = "ix_device_types_name";
    }
}
