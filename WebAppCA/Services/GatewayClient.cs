using Grpc.Net.Client;
using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Grpc.Core;
using System.Threading.Tasks;

namespace WebAppCA.Services
{
    public class GatewayClient
    {
        private GrpcChannel _channel;
        private readonly ILogger<GatewayClient> _logger;
        public GrpcChannel Channel => _channel;
        public bool IsConnected { get; private set; }
        public string LastErrorMessage { get; private set; }
        public string CurrentAddress { get; private set; }
        public int CurrentPort { get; private set; }

        public GatewayClient(ILogger<GatewayClient> logger = null)
        {
            _logger = logger;
            IsConnected = false;
        }

        public bool Connect(string caCertPath, string address, int port)
        {
            try
            {
                _logger?.LogInformation($"Tentative de connexion gRPC à {address}:{port}");
                CurrentAddress = address;
                CurrentPort = port;

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

                // Test la connexion en vérifiant l'état du canal
                var state = _channel.State;
                _logger?.LogInformation($"État initial du canal gRPC: {state}");

                // Tester la connexion en effectuant un ping
                if (TestConnection())
                {
                    IsConnected = true;
                    _logger?.LogInformation("Connexion gRPC établie avec succès");
                    return true;
                }
                else
                {
                    _logger?.LogWarning("La connexion gRPC a été établie mais le test a échoué");
                    _channel = null;
                    IsConnected = false;
                    return false;
                }
            }
            catch (Exception ex)
            {
                LastErrorMessage = ex.Message;
                _logger?.LogError(ex, $"Erreur lors de la connexion gRPC: {ex.Message}");
                _channel = null;
                IsConnected = false;
                return false;
            }
        }

        private bool TestConnection()
        {
            try
            {
                var connectionTask = Task.Run(async () => {
                    await _channel.ConnectAsync();
                    return _channel.State == Grpc.Core.ConnectivityState.Ready ||
                           _channel.State == Grpc.Core.ConnectivityState.Idle;
                });

                // Timeout de 5 secondes pour le test
                var result = connectionTask.Wait(TimeSpan.FromSeconds(5));

                if (!result)
                {
                    _logger?.LogWarning("Timeout lors du test de connexion gRPC");
                    return false;
                }

                return connectionTask.Result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Erreur lors du test de connexion gRPC: {ex.Message}");
                return false;
            }
        }

        public void Reconnect()
        {
            if (!string.IsNullOrEmpty(CurrentAddress) && CurrentPort > 0)
            {
                Close();
                Connect(null, CurrentAddress, CurrentPort);
            }
        }

        public void Close()
        {
            _channel?.Dispose();
            _channel = null;
            IsConnected = false;
        }
    }
}