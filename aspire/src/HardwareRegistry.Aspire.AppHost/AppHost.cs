using Projects;

namespace HardwareRegistry.Aspire.AppHost
{
    internal static class AppHost
    {
        private const string ProjectName = "hardwareregistry";

        private const int AngularClientPort = 4200;
        private const int JQueryClientPort = 4300;

        private static void Main(string[] args)
        {
            var builder = DistributedApplication.CreateBuilder(args);

            var database = builder.AddDatabase(AppHost.ProjectName);

            // Runs to completion before the API starts, so the schema is always current
            // by the time the first request arrives.
            var migrator = builder
                .AddProject<HardwareRegistry_DatabaseMigrator>("migrator")
                .WithReference(database.ConnectionString)
                .WaitFor(database.Database);

            var api = builder
                .AddProject<HardwareRegistry_Api>("api")
                .WithUrlForEndpoint(
                    "http",
                    _ => new()
                    {
                        Url = "/swagger",
                        DisplayText = "Swagger"
                    }
                )
                .WithReference(database.ConnectionString)
                .WaitForCompletion(migrator)
                .WithExternalHttpEndpoints();

            // Two interchangeable clients over the same API.
            builder.AddClient("web-angular", "../../../client", AppHost.AngularClientPort, api);
            builder.AddClient("web-jquery", "../../../client-jquery", AppHost.JQueryClientPort, api);

            builder.Build().Run();
        }

        extension(IDistributedApplicationBuilder builder)
        {
            private void AddClient(
                string name,
                string workingDirectory,
                int port,
                IResourceBuilder<ProjectResource> api
            )
            {
                var client = builder
                    .AddJavaScriptApp(name, workingDirectory, "start")
                    .WithNpm(installCommand: "ci")
                    .WithEnvironment("HARDWARE_REGISTRY_API_URL", api.GetEndpoint("http"))
                    .WithHttpEndpoint(port: port)
                    .WithHttpHealthCheck("/")
                    .WaitFor(api)
                    .WithExternalHttpEndpoints();

                client.WithArgs("--", "--port", client.GetEndpoint("http").Property(EndpointProperty.TargetPort));
            }

            private (
                IResourceBuilder<PostgresDatabaseResource> Database,
                IResourceBuilder<ConnectionStringResource> ConnectionString
                ) AddDatabase(string databaseName)
            {
                var pgPassword = builder.AddParameter("pg-password", secret: true);

                var database = builder
                    .AddPostgres("postgres", password: pgPassword)
                    .WithDataVolume($"{databaseName}-postgres-data")
                    .WithLifetime(ContainerLifetime.Session)
                    .AddDatabase($"{databaseName}-database", databaseName);

                var connectionString = builder.AddConnectionString(
                    databaseName,
                    ReferenceExpression.Create(
                        $"{database};Include Error Detail=true"
                    )
                );

                return (database, connectionString);
            }
        }
    }
}
