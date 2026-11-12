using Npgsql;
using Testcontainers.PostgreSql;

namespace HardwareRegistry.IntegrationTests.Infrastructure
{
    /// <summary>
    /// The PostgreSQL server the tests run against: an existing one when
    /// <see cref="ConnectionStringVariable"/> is set, otherwise a disposable container.
    /// </summary>
    internal sealed class PostgresServer : IAsyncDisposable
    {
        public const string ConnectionStringVariable = "HARDWARE_REGISTRY_TEST_POSTGRES";

        private const string Image = "postgres:17-alpine";

        private readonly PostgreSqlContainer? _container;

        private PostgresServer(string connectionString, PostgreSqlContainer? container)
        {
            this.ConnectionString = connectionString;
            this._container = container;
        }

        /// <summary>Connection string to the server's maintenance database.</summary>
        public string ConnectionString { get; }

        public static async Task<PostgresServer> StartAsync()
        {
            var connectionString = Environment.GetEnvironmentVariable(PostgresServer.ConnectionStringVariable);

            if (!String.IsNullOrWhiteSpace(connectionString))
                return new(connectionString, null);

            var container = new PostgreSqlBuilder(PostgresServer.Image).Build();
            await container.StartAsync();

            return new(container.GetConnectionString(), container);
        }

        public string GetDatabaseConnectionString(string database)
            => new NpgsqlConnectionStringBuilder(this.ConnectionString)
            {
                Database = database,
                IncludeErrorDetail = true
            }.ConnectionString;

        public async ValueTask DisposeAsync()
        {
            if (this._container is not null)
                await this._container.DisposeAsync();
        }
    }
}
