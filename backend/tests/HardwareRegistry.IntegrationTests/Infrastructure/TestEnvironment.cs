using HardwareRegistry.Api.Controllers;
using HardwareRegistry.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace HardwareRegistry.IntegrationTests.Infrastructure
{
    /// <summary>
    /// One migrated database and one running API for the whole test assembly.
    /// The database gets a unique name, so a shared server is never polluted.
    /// </summary>
    public sealed class TestEnvironment : IAsyncLifetime
    {
        private PostgresServer? _server;
        private string? _databaseName;
        private WebApplicationFactory<DevicesController>? _factory;

        public string ConnectionString { get; private set; } = String.Empty;

        public WebApplicationFactory<DevicesController> Factory
            => this._factory ?? throw new InvalidOperationException("The environment is not initialized.");

        public async ValueTask InitializeAsync()
        {
            this._server = await PostgresServer.StartAsync();
            this._databaseName = $"hardwareregistry_tests_{Guid.NewGuid():N}";

            await this.ExecuteOnServerAsync($"CREATE DATABASE {this._databaseName}");

            this.ConnectionString = this._server.GetDatabaseConnectionString(this._databaseName);

            await using (var context = this.CreateDbContext())
                await context.Database.MigrateAsync();

            this._factory = new WebApplicationFactory<DevicesController>()
                .WithWebHostBuilder(builder => builder
                    .UseEnvironment("Development")
                    .UseSetting("ConnectionStrings:hardwareregistry", this.ConnectionString)
                );

            // Starts the host, and with it the device types seeder.
            _ = this._factory.Server;
        }

        public ApplicationDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(this.ConnectionString)
                .UseSnakeCaseNamingConvention()
                .Options;

            return new(options);
        }

        public AsyncServiceScope CreateScope()
            => this.Factory.Services.CreateAsyncScope();

        public async ValueTask DisposeAsync()
        {
            if (this._factory is not null)
                await this._factory.DisposeAsync();

            if (this._server is null)
                return;

            NpgsqlConnection.ClearAllPools();

            if (this._databaseName is not null)
                await this.ExecuteOnServerAsync($"DROP DATABASE IF EXISTS {this._databaseName} WITH (FORCE)");

            await this._server.DisposeAsync();
        }

        private async Task ExecuteOnServerAsync(string sql)
        {
            await using var connection = new NpgsqlConnection(this._server!.ConnectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }
    }
}
