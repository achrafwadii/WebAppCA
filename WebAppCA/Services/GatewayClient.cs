using Grpc.Net.Client;
using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;

namespace WebAppCA.Services
{
    public class GatewayClient
    {
        private GrpcChannel _channel;
        private readonly ILogger<GatewayClient> _logger;
        public GrpcChannel Channel => _channel;
        public bool IsConnected => _channel != null;
        public string LastErrorMessage { get; private set; }

        public GatewayClient(ILogger<GatewayClient> logger = null)
        {
            _logger = logger;
        }

        public bool Connect(string caCertPath, string address, int port)
        {
            try
            {
                _logger?.LogInformation($"Tentative de connexion gRPC à {address}:{port}");

                // Configuration du HttpClientHandler
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };

                // Configure SSL if using a CA certificate
                if (!string.IsNullOrEmpty(caCertPath) && File.Exists(caCertPath))
                {
                    _logger?.LogInformation($"Utilisation du certificat: {caCertPath}");
                    var caCert = new X509Certificate2(caCertPath);
                    handler.ClientCertificates.Add(caCert);
                }
                else
                {
                    _logger?.LogWarning("Aucun certificat trouvé ou spécifié. Connexion sans certificat.");
                }

                var httpClient = new HttpClient(handler);

                // Définir un timeout pour éviter de bloquer trop longtemps
                httpClient.Timeout = TimeSpan.FromSeconds(10);

                var options = new GrpcChannelOptions
                {
                    HttpClient = httpClient,
                };

                _channel = GrpcChannel.ForAddress($"https://{address}:{port}", options);

                // Test de connexion
                var state = _channel.State;
                _logger?.LogInformation($"État du canal gRPC: {state}");

                return true;
            }
            catch (Exception ex)
            {
                LastErrorMessage = ex.Message;
                _logger?.LogError(ex, $"Erreur lors de la connexion gRPC: {ex.Message}");
                _channel = null;
                return false;
            }
        }

        public void Close()
        {
            _channel?.Dispose();
            _channel = null;
        }
    }
}
