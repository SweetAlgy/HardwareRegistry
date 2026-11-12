using System.Text.RegularExpressions;
using HardwareRegistry.Domain.Devices;

namespace HardwareRegistry.Domain.UnitTests.Devices
{
    public sealed class DeviceConstantsTests
    {
        [Theory]
        [InlineData("1")]
        [InlineData("PC1")]
        [InlineData("abcXYZ0123456789")]
        [InlineData("A1B2C3D4E5F6G7H8I9J0K1L2M3N4O5P6")]
        public void InventoryNumberPattern_DigitsAndLatinLetters_Matches(string inventoryNumber)
            => Assert.Matches(DeviceConstants.InventoryNumberPattern, inventoryNumber);

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("PC 1")]
        [InlineData("PC-1")]
        [InlineData("PC_1")]
        [InlineData("ПК1")]
        [InlineData("café")]
        [InlineData("１２３")]
        public void InventoryNumberPattern_OtherCharacters_DoesNotMatch(string inventoryNumber)
            => Assert.DoesNotMatch(DeviceConstants.InventoryNumberPattern, inventoryNumber);

        [Fact]
        public void InventoryNumberPattern_IsAnchoredOnBothEnds()
        {
            var regex = new Regex(DeviceConstants.InventoryNumberPattern);

            Assert.False(regex.IsMatch("PC1!"));
            Assert.False(regex.IsMatch("!PC1"));
        }

        [Fact]
        public void Limits_MatchSpecification()
        {
            Assert.Equal(2000, DeviceConstants.MaxCount);
            Assert.Equal(32, DeviceConstants.InventoryNumberMaxLength);
            Assert.Equal(256, DeviceConstants.NameMaxLength);
            Assert.Equal(1, DeviceConstants.RoomMin);
            Assert.Equal(1000, DeviceConstants.RoomMax);
        }
    }
}
