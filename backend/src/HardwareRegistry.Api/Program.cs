using HardwareRegistry.Api.Extensions;
using HardwareRegistry.Application;
using HardwareRegistry.Infrastructure;
using HardwareRegistry.ServiceDefaults;

namespace HardwareRegistry.Api
{
    internal static class Program
    {
        internal static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.AddServiceDefaults();

            builder.Logging.ClearProviders();
            builder.Logging.AddSimpleConsole(options =>
                {
                    options.IncludeScopes = true;
                    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
                }
            );

            builder.AddInfrastructure();
            builder.Services
                .AddApplication()
                .AddApi();

            var application = builder.Build();
            application.UseApi();
            await application.RunAsync();
        }
    }
}