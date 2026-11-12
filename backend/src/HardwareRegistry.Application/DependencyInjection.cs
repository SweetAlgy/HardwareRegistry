using HardwareRegistry.Application.Devices;
using HardwareRegistry.Application.Devices.Abstractions;
using HardwareRegistry.Application.DeviceTypes;
using HardwareRegistry.Application.DeviceTypes.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HardwareRegistry.Application
{
    public static class DependencyInjection
    {
        extension(IServiceCollection services)
        {
            public IServiceCollection AddApplication()
            {
                services.TryAddSingleton(TimeProvider.System);

                return services.AddDeviceServices();
            }

            private IServiceCollection AddDeviceServices()
            {
                services.AddScoped<IDevicesService, DevicesService>();
                services.AddScoped<IDeviceTypesService, DeviceTypesService>();

                return services;
            }
        }
    }
}
