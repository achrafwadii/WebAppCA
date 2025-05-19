using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using connect; // Namespace généré par G-SDK pour le service Connect

namespace MyApp.Services
{
    public class DeviceGatewayService : BackgroundService
    {
        private readonly ILogger<DeviceGatewayService> _logger;
        private readonly string _caCertPath;
        private readonly string _gatewayAddress;
        private readonly int _gatewayPort;

        public DeviceGatewayService(ILogger<DeviceGatewayService> logger, IConfiguration configuration)
        {
            _logger = logger;
            // Récupère les paramètres depuis la configuration (appsettings.json ou autre)
            _caCertPath = configuration["DeviceGateway:CaCertPath"];
            _gatewayAddress = configuration["DeviceGateway:Address"];
            _gatewayPort = int.Parse(configuration["DeviceGateway:Port"]);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Channel channel = null;
            try
            {
                // Crée un canal gRPC sécurisé vers le Device Gateway
                var channelCredentials = new SslCredentials(File.ReadAllText(_caCertPath));
                channel = new Channel(_gatewayAddress, _gatewayPort, channelCredentials);
                _logger.LogInformation("Connecté au Device Gateway à {Address}:{Port}", _gatewayAddress, _gatewayPort);

                // Initialise le client Connect pour communiquer avec les appareils via le gateway
                var connectClient = new connect.Connect.ConnectClient(channel);

                // Souscription aux changements de statut des appareils
                var subscribeRequest = new SubscribeStatusRequest { QueueSize = 100 };
                using var call = connectClient.SubscribeStatus(subscribeRequest);
                _logger.LogInformation("Abonné aux mises à jour de statut des appareils");

                // Lecture continue des StatusChange tant que le service tourne
                while (await call.ResponseStream.MoveNext(stoppingToken))
                {
                    var statusChange = call.ResponseStream.Current;
                    _logger.LogInformation(
                        "Appareil {DeviceID} statut changé : {Status} à {Timestamp}",
                        statusChange.DeviceID, statusChange.Status, statusChange.Timestamp
                    );
                }
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, "Erreur gRPC lors de la connexion ou de l'écoute du Device Gateway");
            }
            catch (OperationCanceledException)
            {
                // Arrêt du service demandé : pas de log d'erreur nécessaire
                _logger.LogInformation("DeviceGatewayService a été annulé.");
            }
            finally
            {
                if (channel != null)
                {
                    await channel.ShutdownAsync();
                    _logger.LogInformation("Déconnecté du Device Gateway");
                }
            }
        }
    }
}
