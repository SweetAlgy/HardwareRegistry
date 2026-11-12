using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using HardwareRegistry.Infrastructure;
using HardwareRegistry.Infrastructure.Persistence;
using HardwareRegistry.ServiceDefaults;

namespace HardwareRegistry.DatabaseMigrator
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            builder.AddBaseInfrastructure();
            builder.AddServiceDefaults();

            using var host = builder.Build();

            await host.StartAsync();

            try
            {
                await using var scope = host.Services.CreateAsyncScope();

                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var applicationLifeTime = scope.ServiceProvider.GetRequiredService<IHostApplicationLifetime>();

                var logger = scope.ServiceProvider
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger(nameof(Program));

                logger.LogInformation("Applying database migrations");

                await dbContext.Database.MigrateAsync(applicationLifeTime.ApplicationStopping);

                logger.LogInformation("Database migrations applied successfully");
            }
            finally
            {
                await host.StopAsync();
            }
        }
    }
}