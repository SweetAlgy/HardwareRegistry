namespace HardwareRegistry.Domain.Devices
{
    public static class DeviceConstants
    {
        /// <summary>Upper bound on the number of devices the registry may hold.</summary>
        public const int MaxCount = 2000;

        public const int InventoryNumberMaxLength = 32;

        /// <summary>Digits and Latin letters only.</summary>
        public const string InventoryNumberPattern = "^[A-Za-z0-9]+$";

        public const int NameMaxLength = 256;

        public const int RoomMin = 1;
        public const int RoomMax = 1000;
    }
}
