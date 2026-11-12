using HardwareRegistry.Api.Extensions;
using HardwareRegistry.Application.Common;
using HardwareRegistry.Application.Devices.Abstractions;
using HardwareRegistry.Application.Devices.Requests;
using HardwareRegistry.Application.Devices.Responses;
using Microsoft.AspNetCore.Mvc;

namespace HardwareRegistry.Api.Controllers
{
    [ApiController, Route("api/[controller]")]
    public sealed class DevicesController(IDevicesService devicesService) : ControllerBase
    {
        private readonly IDevicesService _devicesService = devicesService;

        [HttpGet]
        [ProducesResponseType<PagedResult<DeviceResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PagedResult<DeviceResponse>>> GetDevicesAsync(
            [FromQuery] GetDevicesRequest request,
            CancellationToken cancellationToken
        )
        {
            var devicesPage = await this._devicesService.GetDevicesAsync(request, cancellationToken);

            var devices = devicesPage.To(devicesPage.Items.Select(DeviceResponse.FromEntity));

            return this.Ok(devices);
        }

        [HttpGet("{deviceId:guid}", Name = nameof(DevicesController.GetDeviceByIdAsync))]
        [ProducesResponseType<DeviceResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DeviceResponse>> GetDeviceByIdAsync(
            [FromRoute] Guid deviceId,
            CancellationToken cancellationToken
        )
        {
            var deviceResult = await this._devicesService.GetDeviceByIdAsync(deviceId, cancellationToken);

            if (deviceResult.IsFailure)
                return this.Error(deviceResult);

            var device = DeviceResponse.FromEntity(deviceResult.Value);

            return this.Ok(device);
        }

        [HttpPost]
        [ProducesResponseType<DeviceResponse>(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<DeviceResponse>> CreateDeviceAsync(
            [FromBody] SaveDeviceRequest request,
            CancellationToken cancellationToken
        )
        {
            var deviceResult = await this._devicesService.CreateDeviceAsync(request, cancellationToken);

            if (deviceResult.IsFailure)
                return this.Error(deviceResult);

            var device = DeviceResponse.FromEntity(deviceResult.Value);

            return this.CreatedAtRoute(
                nameof(DevicesController.GetDeviceByIdAsync),
                new { deviceId = device.Id },
                device
            );
        }

        [HttpPut("{deviceId:guid}")]
        [ProducesResponseType<DeviceResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<DeviceResponse>> UpdateDeviceAsync(
            [FromRoute] Guid deviceId,
            [FromBody] SaveDeviceRequest request,
            CancellationToken cancellationToken
        )
        {
            var deviceResult = await this._devicesService.UpdateDeviceAsync(deviceId, request, cancellationToken);

            if (deviceResult.IsFailure)
                return this.Error(deviceResult);

            var device = DeviceResponse.FromEntity(deviceResult.Value);

            return this.Ok(device);
        }

        [HttpDelete("{deviceId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteDeviceAsync(
            [FromRoute] Guid deviceId,
            CancellationToken cancellationToken
        )
        {
            var result = await this._devicesService.DeleteDeviceAsync(deviceId, cancellationToken);

            if (result.IsFailure)
                return this.Error(result);

            return this.NoContent();
        }
    }
}
