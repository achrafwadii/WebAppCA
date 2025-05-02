using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WebAppCA.Services;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using System.Threading;

namespace WebAppCA.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddGrpcServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Enregistrer GatewayClient comme Singleton
            services.AddSingleton<GatewayClient>();

            // Enregistrer le service de surveillance de connexion gRPC
            services.AddHostedService<GrpcConnectionWatchdog>();

            // Enregistrer ConnectSvc comme service Scoped avec une factory
            services.AddScoped<ConnectSvc>(provider =>
            {
                var gatewayClient = provider.GetRequiredService<GatewayClient>();
                var logger = provider.GetRequiredService<ILogger<ConnectSvc>>();

                // Vérifier si le client Gateway est connecté avant de créer ConnectSvc
                if (!gatewayClient.IsConnected || gatewayClient.Channel == null)
                {
                    logger.LogInformation("Tentative d'initialisation de la connexion gRPC");

                    var certPath = configuration.GetValue<string>("GrpcSettings:CaCertPath") ?? "";
                    var address = configuration.GetValue<string>("GrpcSettings:Address") ?? "localhost";
                    var port = configuration.GetValue<int>("GrpcSettings:Port", 51211);

                    logger.LogInformation("Configuration gRPC: Adresse={Address}, Port={Port}, CertPath={CertPath}",
                        address, port, certPath);

                    var connected = gatewayClient.Connect(certPath, address, port);

                    if (!connected)
                    {
                        logger.LogWarning("Impossible de se connecter au service gRPC, le service ConnectSvc sera en mode dégradé");
                    }
                }

                return new ConnectSvc(gatewayClient.Channel, logger);
            });

            return services;
        }
    }

    // Service d'arrière-plan pour surveiller la connexion gRPC
    public class GrpcConnectionWatchdog : BackgroundService
    {
        private readonly GatewayClient _gatewayClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GrpcConnectionWatchdog> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5); // Vérifier toutes les 5 minutes

        public GrpcConnectionWatchdog(
            GatewayClient gatewayClient,
            IConfiguration configuration,
            ILogger<GrpcConnectionWatchdog> logger)
        {
            _gatewayClient = gatewayClient;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Service de surveillance de connexion gRPC démarré");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Vérifier l'état de la connexion
                    if (!_gatewayClient.IsConnected)
                    {
                        _logger.LogWarning("Connexion gRPC perdue, tentative de reconnexion...");

                        var certPath = _configuration.GetValue<string>("GrpcSettings:CaCertPath") ?? "";
                        var address = _configuration.GetValue<string>("GrpcSettings:Address") ?? "localhost";
                        var port = _configuration.GetValue<int>("GrpcSettings:Port", 51211);

                        var connected = _gatewayClient.Connect(certPath, address, port);

                        if (connected)
                        {
                            _logger.LogInformation("Reconnexion gRPC réussie");
                        }
                        else
                        {
                            _logger.LogError("Échec de la tentative de reconnexion gRPC");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la vérification de la connexion gRPC");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("Service de surveillance de connexion gRPC arrêté");
        }
    }
}