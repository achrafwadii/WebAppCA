using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WebAppCA.Services;
using System;

namespace WebAppCA.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddGrpcServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Enregistrer GatewayClient comme Singleton
            services.AddSingleton<GatewayClient>();

            // Enregistrer ConnectSvc comme service Scoped avec une factory
            services.AddScoped<ConnectSvc>(provider =>
            {
                var gatewayClient = provider.GetRequiredService<GatewayClient>();
                var logger = provider.GetRequiredService<ILogger<ConnectSvc>>();

                // Vérifier si le client Gateway est connecté avant de créer ConnectSvc
                if (gatewayClient.Channel == null)
                {
                    logger.LogInformation("Initialisation de la connexion gRPC");

                    var certPath = configuration.GetValue<string>("GrpcSettings:CaCertPath") ?? "";
                    var address = configuration.GetValue<string>("GrpcSettings:Address") ?? "localhost";
                    var port = configuration.GetValue<int>("GrpcSettings:Port", 51211);

                    logger.LogInformation("Configuration gRPC: Adresse={Address}, Port={Port}, CertPath={CertPath}",
                        address, port, certPath);

                    var connected = gatewayClient.Connect(certPath, address, port);

                    if (!connected)
                    {
                        logger.LogWarning("Impossible de se connecter au service gRPC, le service ConnectSvc sera limité");
                    }
                }

                return new ConnectSvc(gatewayClient.Channel, logger);
            });

            return services;
        }
    }
}