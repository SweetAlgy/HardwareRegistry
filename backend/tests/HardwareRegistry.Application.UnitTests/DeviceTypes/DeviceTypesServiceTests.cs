using HardwareRegistry.Application.DeviceTypes;
using HardwareRegistry.Application.DeviceTypes.Abstractions;
using HardwareRegistry.Domain.DeviceTypes;
using NSubstitute;

namespace HardwareRegistry.Application.UnitTests.DeviceTypes
{
    public sealed class DeviceTypesServiceTests
    {
        [Fact]
        public async Task GetDeviceTypesAsync_Dictionary_ReturnsRepositoryItems()
        {
            DeviceTypeEntity[] deviceTypes = [new() { Name = "Монитор" }, new() { Name = "Ноутбук" }];
            var repository = Substitute.For<IDeviceTypesRepository>();
            repository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(deviceTypes);
            var service = new DeviceTypesService(repository);

            var result = await service.GetDeviceTypesAsync(TestContext.Current.CancellationToken);

            Assert.Equal(deviceTypes, result);
        }

    }
}
