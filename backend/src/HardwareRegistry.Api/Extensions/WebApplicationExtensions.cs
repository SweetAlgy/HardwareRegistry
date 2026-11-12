using HardwareRegistry.Api.Hubs;
using HardwareRegistry.ServiceDefaults;
using SwaggerThemes;

namespace HardwareRegistry.Api.Extensions
{
    internal static class WebApplicationExtensions
    {
        public static WebApplication UseApi(this WebApplication application)
        {
            application.MapDefaultEndpoints();

            if (application.Environment.IsDevelopment())
            {
                application.UseSwagger();
                application.UseSwaggerUI(Theme.Vs2022);
            }

            application.UseExceptionHandler();

            application.MapControllers();
            application.MapHub<DevicesHub>(DevicesHub.Route);

            return application;
        }
    }
}
