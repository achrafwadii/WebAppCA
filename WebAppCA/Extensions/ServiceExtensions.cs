using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using WebAppCA.Services;
using System;

namespace WebAppCA.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddGrpcServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Register GatewayClient as a singleton
            services.AddSingleton<GatewayClient>();

            // Register ConnectSvc as a scoped service with a factory that resolves GrpcChannel from GatewayClient
            services.AddScoped<ConnectSvc>(provider =>
            {
                var gatewayClient = provider.GetRequiredService<GatewayClient>();

                // Make sure the GatewayClient is connected before creating ConnectSvc
                if (gatewayClient.Channel == null)
                {
                    var certPath = configuration.GetValue<string>("GrpcSettings:CaCertPath") ?? "";
                    var address = configuration.GetValue<string>("GrpcSettings:Address") ?? "localhost";
                    var port = configuration.GetValue<int>("GrpcSettings:Port", 51211);

                    gatewayClient.Connect(certPath, address, port);
                }

                return new ConnectSvc(gatewayClient.Channel);
            });

            return services;
        }
    }
}