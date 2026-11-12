using HardwareRegistry.Api.Controllers;
using HardwareRegistry.Application.Common;
using HardwareRegistry.Application.Common.Errors;
using HardwareRegistry.Application.Devices.Abstractions;
using HardwareRegistry.Application.Devices.Requests;
using HardwareRegistry.Application.Devices.Responses;
using HardwareRegistry.Domain.Devices;
using HardwareRegistry.Domain.DeviceTypes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace HardwareRegistry.Api.UnitTests.Controllers
{
    public sealed class DevicesControllerTests
    {
        private readonly IDevicesService _devicesService = Substitute.For<IDevicesService>();
        private readonly DevicesController _controller;

        private readonly DeviceEntity _device;

        public DevicesControllerTests()
        {
            this._controller = new(this._devicesService)
            {
                ControllerContext = new() { HttpContext = new DefaultHttpContext() }
            };

            var deviceType = new DeviceTypeEntity { Name = "Ноутбук" };
            this._device = new(DateTime.UtcNow)
            {
                InventoryNumber = "NB1",
                Name = "ThinkPad",
                TypeId = deviceType.Id,
                Type = deviceType,
                Room = 12
            };
        }

        private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

        [Fact]
        public async Task GetDevicesAsync_Page_ReturnsMappedPage()
        {
            var request = new GetDevicesRequest();
            this._devicesService
                .GetDevicesAsync(request, Arg.Any<CancellationToken>())
                .Returns(new PagedResult<DeviceEntity> { Items = [this._device], Page = 1, PageSize = 10, TotalCount = 1 });

            var actionResult = await this._controller.GetDevicesAsync(request, DevicesControllerTests.CancellationToken);

            var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            var page = Assert.IsType<PagedResult<DeviceResponse>>(ok.Value);
            var item = Assert.Single(page.Items);
            Assert.Equal("NB1", item.InventoryNumber);
            Assert.Equal("Ноутбук", item.TypeName);
            Assert.Equal(1, page.TotalCount);
        }

        [Fact]
        public async Task CreateDeviceAsync_Success_ReturnsCreatedAtGetRoute()
        {
            var request = DevicesControllerTests.CreateRequest();
            this._devicesService.CreateDeviceAsync(request, Arg.Any<CancellationToken>()).Returns(this._device);

            var actionResult = await this._controller.CreateDeviceAsync(request, DevicesControllerTests.CancellationToken);

            var created = Assert.IsType<CreatedAtRouteResult>(actionResult.Result);
            Assert.Equal(nameof(DevicesController.GetDeviceByIdAsync), created.RouteName);
            Assert.Equal(this._device.Id, created.RouteValues?["deviceId"]);
            Assert.Equal(this._device.Id, Assert.IsType<DeviceResponse>(created.Value).Id);
        }

        [Fact]
        public async Task CreateDeviceAsync_Failure_ReturnsProblem()
        {
            var request = DevicesControllerTests.CreateRequest();
            this._devicesService
                .CreateDeviceAsync(request, Arg.Any<CancellationToken>())
                .Returns(OperationErrors.LimitExceeded("devices", DeviceConstants.MaxCount));

            var actionResult = await this._controller.CreateDeviceAsync(request, DevicesControllerTests.CancellationToken);

            var problem = Assert.IsType<ObjectResult>(actionResult.Result);
            Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
        }

        [Fact]
        public async Task GetDeviceByIdAsync_Missing_ReturnsNotFoundProblem()
        {
            var deviceId = Guid.NewGuid();
            this._devicesService
                .GetDeviceByIdAsync(deviceId, Arg.Any<CancellationToken>())
                .Returns(EntityErrors<DeviceEntity>.NotFoundById(deviceId));

            var actionResult = await this._controller.GetDeviceByIdAsync(deviceId, DevicesControllerTests.CancellationToken);

            var problem = Assert.IsType<ObjectResult>(actionResult.Result);
            Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
        }

        [Fact]
        public async Task UpdateDeviceAsync_Success_ReturnsOkWithDevice()
        {
            var request = DevicesControllerTests.CreateRequest();
            this._devicesService
                .UpdateDeviceAsync(this._device.Id, request, Arg.Any<CancellationToken>())
                .Returns(this._device);

            var actionResult = await this._controller.UpdateDeviceAsync(
                this._device.Id,
                request,
                DevicesControllerTests.CancellationToken
            );

            var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            Assert.Equal(this._device.Id, Assert.IsType<DeviceResponse>(ok.Value).Id);
        }

        [Fact]
        public async Task DeleteDeviceAsync_Success_ReturnsNoContent()
        {
            this._devicesService
                .DeleteDeviceAsync(this._device.Id, Arg.Any<CancellationToken>())
                .Returns(Result.Success());

            var actionResult = await this._controller.DeleteDeviceAsync(this._device.Id, DevicesControllerTests.CancellationToken);

            Assert.IsType<NoContentResult>(actionResult);
        }

        [Fact]
        public async Task DeleteDeviceAsync_Missing_ReturnsNotFoundProblem()
        {
            this._devicesService
                .DeleteDeviceAsync(this._device.Id, Arg.Any<CancellationToken>())
                .Returns(EntityErrors<DeviceEntity>.NotFoundById(this._device.Id));

            var actionResult = await this._controller.DeleteDeviceAsync(this._device.Id, DevicesControllerTests.CancellationToken);

            Assert.Equal(StatusCodes.Status404NotFound, Assert.IsType<ObjectResult>(actionResult).StatusCode);
        }

        private static SaveDeviceRequest CreateRequest()
            => new() { InventoryNumber = "NB1", Name = "ThinkPad", TypeId = Guid.NewGuid(), Room = 12 };
    }
}
