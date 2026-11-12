using HardwareRegistry.Application.Abstractions.Persistence;
using HardwareRegistry.Application.Devices.Abstractions;
using HardwareRegistry.Application.DeviceTypes.Abstractions;
using HardwareRegistry.Infrastructure.Persistence;
using HardwareRegistry.Infrastructure.Persistence.Postgres;
using HardwareRegistry.Infrastructure.Persistence.Repositories;
using HardwareRegistry.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HardwareRegistry.Infrastructure
{
    public static class DependencyInjection
    {
        private const string DatabaseConnectionName = "hardwareregistry";

        /// <summary>
        /// Conventions every <see cref="ApplicationDbContext"/> must be built with, wherever it is
        /// composed of: the migrations were generated with the snake_case convention, so a context
        /// without it cannot even read __EFMigrationsHistory.
        /// </summary>
        internal static void ConfigureDbContext(DbContextOptionsBuilder options)
            => options.UseSnakeCaseNamingConvention();

        extension(IHostApplicationBuilder builder)
        {
            public IHostApplicationBuilder AddBaseInfrastructure()
            {
                builder.AddNpgsqlDbContext<ApplicationDbContext>(
                    DependencyInjection.DatabaseConnectionName,
                    configureDbContextOptions: DependencyInjection.ConfigureDbContext
                );

                builder.Services.AddSingleton(TimeProvider.System);
                return builder;
            }

            public IHostApplicationBuilder AddInfrastructure()
            {
                builder.Services
                    .AddDatabase(builder.Configuration)
                    .AddDeviceRepositories();

                return builder;
            }
        }

        extension(IServiceCollection services)
        {
            private IServiceCollection AddDatabase(IConfiguration configuration)
            {
                if (String.IsNullOrWhiteSpace(
                        configuration.GetConnectionString(DependencyInjection.DatabaseConnectionName)
                    ))
                    throw new InvalidOperationException("Connection string 'hardwareregistry' is not configured.");

                services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
                    {
                        var connectionString = serviceProvider
                            .GetRequiredService<IConfiguration>()
                            .GetConnectionString(DependencyInjection.DatabaseConnectionName);

                        options.UseNpgsql(connectionString);
                        DependencyInjection.ConfigureDbContext(options);
                    }
                );

                services.AddScoped<IUnitOfWork, UnitOfWork>();
                return services;
            }

            private IServiceCollection AddDeviceRepositories()
            {
                services.AddScoped<IDevicesRepository, DevicesRepository>();
                services.AddScoped<IDeviceTypesRepository, DeviceTypesRepository>();
                services.AddScoped<IDeviceRegistryLock, PostgresDeviceRegistryLock>();

                services.AddHostedService<DeviceTypesSeeder>();
                return services;
            }
        }
    }
}
