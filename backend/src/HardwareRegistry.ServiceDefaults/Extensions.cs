using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace HardwareRegistry.ServiceDefaults
{
    // Adds common Aspire services: service discovery, resilience, health checks, and OpenTelemetry.
    // This project should be referenced by each service project in your solution.
    // To learn more about using this project, see https://aka.ms/aspire/service-defaults
    public static class Extensions
    {
        private const string HealthEndpointPath = "/health";
        private const string AlivenessEndpointPath = "/alive";

        private static readonly HashSet<int> AllowedHealthCheckPorts = [4200, 5192];

        extension<TBuilder>(TBuilder builder) where TBuilder : IHostApplicationBuilder
        {
            public TBuilder AddServiceDefaults()
            {
                builder.ConfigureOpenTelemetry();

                builder.AddDefaultHealthChecks();

                builder.Services.AddServiceDiscovery();

                builder.Services.ConfigureHttpClientDefaults(http =>
                    {
                        // Turn on resilience by default
                        http.AddStandardResilienceHandler();

                        // Turn on service discovery by default
                        http.AddServiceDiscovery();
                    }
                );

                // Uncomment the following to restrict the allowed schemes for service discovery.
                // builder.Services.Configure<ServiceDiscoveryOptions>(options =>
                // {
                //     options.AllowedSchemes = ["https"];
                // });

                return builder;
            }

            public TBuilder ConfigureOpenTelemetry()
            {
                builder.Logging.AddOpenTelemetry(logging =>
                    {
                        logging.IncludeFormattedMessage = true;
                        logging.IncludeScopes = true;
                    }
                );

                builder.Services.AddOpenTelemetry()
                    .WithMetrics(metrics =>
                        {
                            metrics.AddAspNetCoreInstrumentation()
                                .AddHttpClientInstrumentation()
                                .AddRuntimeInstrumentation();
                        }
                    )
                    .WithTracing(tracing =>
                        {
                            tracing.AddSource(builder.Environment.ApplicationName)
                                .AddAspNetCoreInstrumentation(options =>
                                    // Exclude health check requests from tracing
                                    options.Filter = context =>
                                        !context.Request.Path.StartsWithSegments(Extensions.HealthEndpointPath)
                                        && !context.Request.Path.StartsWithSegments(Extensions.AlivenessEndpointPath)
                                )
                                // Uncomment the following line to enable gRPC instrumentation (requires the OpenTelemetry.Instrumentation.GrpcNetClient package)
                                //.AddGrpcClientInstrumentation()
                                .AddHttpClientInstrumentation();
                        }
                    );

                builder.AddOpenTelemetryExporters();

                return builder;
            }

            private TBuilder AddOpenTelemetryExporters()
            {
                var useOtlpExporter = !String.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

                if (useOtlpExporter)
                {
                    builder.Services.AddOpenTelemetry().UseOtlpExporter();
                }

                // Uncomment the following lines to enable the Azure Monitor exporter (requires the Azure.Monitor.OpenTelemetry.AspNetCore package)
                //if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
                //{
                //    builder.Services.AddOpenTelemetry()
                //       .UseAzureMonitor();
                //}

                return builder;
            }

            public TBuilder AddDefaultHealthChecks()
            {
                builder.Services.AddRequestTimeouts(
                    configure: static timeouts => timeouts.AddPolicy("HealthChecks", TimeSpan.FromSeconds(5))
                );

                builder.Services.AddOutputCache(
                    configureOptions: static caching => caching.AddPolicy(
                        "HealthChecks",
                        build: static policy => policy.Expire(TimeSpan.FromSeconds(10))
                    )
                );

                builder.Services.AddRateLimiter(static options =>
                    {
                        options.AddFixedWindowLimiter(
                            "HealthChecks",
                            limiter =>
                            {
                                limiter.PermitLimit = 30;
                                limiter.Window = TimeSpan.FromMinutes(1);
                                limiter.QueueLimit = 0;
                            }
                        );

                        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                    }
                );

                builder.Services.AddHealthChecks()
                    // Add a default liveness check to ensure app is responsive
                    .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

                return builder;
            }
        }

        public static WebApplication MapDefaultEndpoints(this WebApplication application)
        {
            application.UseRateLimiter();

            var healthChecks = application.MapGroup("");

            healthChecks
                .CacheOutput("HealthChecks")
                .WithRequestTimeout("HealthChecks")
                .RequireRateLimiting("HealthChecks");

            // All health checks must pass for app to be
            // considered ready to accept traffic after starting
            healthChecks.MapHealthChecks(Extensions.HealthEndpointPath);

            // Only health checks tagged with the "live" tag
            // must pass for app to be considered alive
            healthChecks.MapHealthChecks(
                Extensions.AlivenessEndpointPath,
                new()
                {
                    Predicate = static registration => registration.Tags.Contains("live"),
                }
            );

            return application;
        }
    }
}