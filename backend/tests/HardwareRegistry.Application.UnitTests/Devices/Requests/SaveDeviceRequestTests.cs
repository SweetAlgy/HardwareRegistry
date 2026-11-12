using HardwareRegistry.Application.Devices.Requests;
using HardwareRegistry.Application.UnitTests.TestUtilities;

namespace HardwareRegistry.Application.UnitTests.Devices.Requests
{
    public sealed class SaveDeviceRequestTests
    {
        [Fact]
        public void Validate_ValidCard_IsValid()
            => Assert.Empty(ModelValidator.Validate(SaveDeviceRequestTests.CreateRequest()));

        [Theory]
        [InlineData("1")]
        [InlineData("PC10")]
        [InlineData("abcDEF123")]
        [InlineData("A1B2C3D4E5F6G7H8I9J0K1L2M3N4O5P6")]
        public void Validate_InventoryNumberOfDigitsAndLatinLetters_IsValid(string inventoryNumber)
            => Assert.Empty(ModelValidator.Validate(SaveDeviceRequestTests.CreateRequest(inventoryNumber: inventoryNumber)));

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("PC 1")]
        [InlineData("PC-1")]
        [InlineData("ПК1")]
        [InlineData("café")]
        // .NET's "$" also matches before a trailing newline; the attribute must still reject it.
        [InlineData("PC1\n")]
        [InlineData("A1B2C3D4E5F6G7H8I9J0K1L2M3N4O5P6Q")]
        public void Validate_InvalidInventoryNumber_ReportsInventoryNumber(string inventoryNumber)
            => SaveDeviceRequestTests.AssertInvalid(
                SaveDeviceRequestTests.CreateRequest(inventoryNumber: inventoryNumber),
                nameof(SaveDeviceRequest.InventoryNumber)
            );

        [Fact]
        public void Validate_NameOfMaxLength_IsValid()
            => Assert.Empty(ModelValidator.Validate(SaveDeviceRequestTests.CreateRequest(name: new string('Я', 256))));

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Validate_BlankName_ReportsName(string name)
            => SaveDeviceRequestTests.AssertInvalid(
                SaveDeviceRequestTests.CreateRequest(name: name),
                nameof(SaveDeviceRequest.Name)
            );

        [Fact]
        public void Validate_NameLongerThanMax_ReportsName()
            => SaveDeviceRequestTests.AssertInvalid(
                SaveDeviceRequestTests.CreateRequest(name: new string('Я', 257)),
                nameof(SaveDeviceRequest.Name)
            );

        [Fact]
        public void Validate_NameWithArbitraryCharacters_IsValid()
            => Assert.Empty(ModelValidator.Validate(
                    SaveDeviceRequestTests.CreateRequest(name: "Принтер HP LaserJet #3, кабинет «Бухгалтерия»")
                )
            );

        [Fact]
        public void Validate_MissingType_ReportsTypeId()
            => SaveDeviceRequestTests.AssertInvalid(
                SaveDeviceRequestTests.CreateRequest() with { TypeId = null },
                nameof(SaveDeviceRequest.TypeId)
            );

        [Theory]
        [InlineData(1)]
        [InlineData(500)]
        [InlineData(1000)]
        public void Validate_RoomInRange_IsValid(int room)
            => Assert.Empty(ModelValidator.Validate(SaveDeviceRequestTests.CreateRequest(room: room)));

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(1001)]
        [InlineData(Int32.MaxValue)]
        public void Validate_RoomOutOfRange_ReportsRoom(int room)
            => SaveDeviceRequestTests.AssertInvalid(
                SaveDeviceRequestTests.CreateRequest(room: room),
                nameof(SaveDeviceRequest.Room)
            );

        [Fact]
        public void Validate_MissingRoom_ReportsRoom()
            => SaveDeviceRequestTests.AssertInvalid(
                SaveDeviceRequestTests.CreateRequest() with { Room = null },
                nameof(SaveDeviceRequest.Room)
            );

        private static SaveDeviceRequest CreateRequest(
            string inventoryNumber = "PC1",
            string name = "Workstation",
            int room = 101
        ) => new()
        {
            InventoryNumber = inventoryNumber,
            Name = name,
            TypeId = Guid.NewGuid(),
            Room = room
        };

        private static void AssertInvalid(SaveDeviceRequest request, string expectedMember)
        {
            var errors = ModelValidator.Validate(request);

            Assert.NotEmpty(errors);
            Assert.All(errors, error => Assert.Contains(expectedMember, error.MemberNames));
        }
    }
}
