using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using HardwareRegistry.Api.ErrorHandling;
using HardwareRegistry.Api.Hubs;
using HardwareRegistry.Api.OpenApi;
using HardwareRegistry.Api.Routing;
using HardwareRegistry.Application.Devices.Abstractions;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace HardwareRegistry.Api
{
    public static class DependencyInjection
    {
        extension(IServiceCollection services)
        {
            public IServiceCollection AddApi()
            {
                services.AddProblemDetails();
                services.AddExceptionHandler<GlobalExceptionHandler>();

                services
                    .AddControllers(options =>
                        {
                            options.Conventions.Add(
                                new RouteTokenTransformerConvention(new LowerCaseRouteTransformer())
                            );
                        }
                    )
                    .AddJsonOptions(options =>
                        {
                            options.JsonSerializerOptions.WriteIndented = true;
                            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                        }
                    );

                services
                    .AddSignalR()
                    .AddJsonProtocol(options =>
                        {
                            options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                            options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                        }
                    );

                services.AddSingleton<IDevicesNotifier, SignalRDevicesNotifier>();

                services.AddSwaggerGen(options =>
                    {
                        options.SwaggerDoc("v1", new() { Title = "HardwareRegistry.Api", Version = "v1" });

                        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                        options.IncludeXmlComments(xmlPath);
                        options.EnableAnnotations();
                        options.CustomSchemaIds(SwaggerGenericSchemaFormatter.Format);
                        options.DescribeAllParametersInCamelCase();
                        options.UseInlineDefinitionsForEnums();
                    }
                );

                return services;
            }
        }
    }
}
