using HardwareRegistry.Application.DeviceTypes.Abstractions;
using HardwareRegistry.Application.DeviceTypes.Responses;
using Microsoft.AspNetCore.Mvc;

namespace HardwareRegistry.Api.Controllers
{
    [ApiController, Route("api/device-types")]
    public sealed class DeviceTypesController(IDeviceTypesService deviceTypesService) : ControllerBase
    {
        private readonly IDeviceTypesService _deviceTypesService = deviceTypesService;

        [HttpGet]
        [ProducesResponseType<IReadOnlyList<DeviceTypeResponse>>(StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<DeviceTypeResponse>>> GetDeviceTypesAsync(
            CancellationToken cancellationToken
        )
        {
            var deviceTypeEntities = await this._deviceTypesService.GetDeviceTypesAsync(cancellationToken);

            var deviceTypes = deviceTypeEntities
                .Select(DeviceTypeResponse.FromEntity)
                .ToList();

            return this.Ok(deviceTypes);
        }
    }
}
