using connect;
using Grpc.Core;
using Grpc.Net.Client;
using Grpcdevice;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using static Grpcdevice.Device;

namespace WebAppCA.Services
{
    public class GatewayClient
    {
        readonly ILogger<GatewayClient> _logger;
        private Channel _channel;
        private const int MAX_SIZE_GET_LOG = 1024 * 1024 * 1024;
        public bool IsConnected { get; private set; }
        public GrpcChannel Channel { get; private set; }

        public connect.Connect.ConnectClient ConnectClient { get; private set; }
        public DeviceClient DeviceClient { get; private set; }

        public GatewayClient(ILogger<GatewayClient> logger = null)
        {
            _logger = logger;
        }

        public bool Connect(string caPath, string certPath, string keyPath, string address, int port)
        {
            try
            {
                var cacert = File.ReadAllText(caPath);
                var clientCert = File.ReadAllText(certPath);
                var clientKey = File.ReadAllText(keyPath);

                var credentials = new SslCredentials(cacert, new KeyCertificatePair(clientCert, clientKey));

                var channelOptions = new List<ChannelOption>
                {
                    new ChannelOption("grpc.max_receive_message_length", MAX_SIZE_GET_LOG)
                };

                _channel = new Channel(address, port, credentials, channelOptions);

                InitStubs(_channel);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to connect: {0}", ex.Message);
                return false;
            }
        }

        public bool Connect(string serverAddr, int serverPort)
        {
            try
            {
                // Utiliser des credentials non sécurisés pour les tests
                var credentials = ChannelCredentials.Insecure;

                _channel = new Channel(serverAddr, serverPort, credentials);

                _logger.LogInformation("Canal gRPC créé avec succès vers {ServerAddr}:{ServerPort}", serverAddr, serverPort);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la connexion au serveur gRPC");
                return false;
            }
        }
        public async Task<bool> ConnectWithRetryAsync(string serverAddr, int serverPort, int maxAttempts = 3)
        {
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    if (Connect(serverAddr, serverPort))
                        return true;

                    await Task.Delay(TimeSpan.FromSeconds(2 * attempt));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Tentative {Attempt} échouée", attempt);
                }
            }
            return false;
        }
        public async Task<bool> ConnectWithHttps(string address, int port, bool ignoreCertErrors = false)
        {
            try
            {
                // Configuration sécurisée avec HTTPS
                var handler = new SocketsHttpHandler();

                // Si on souhaite ignorer les erreurs de certificat (pour les environnements de développement uniquement)
                if (ignoreCertErrors)
                {
                    handler.SslOptions = new System.Net.Security.SslClientAuthenticationOptions
                    {
                        RemoteCertificateValidationCallback = delegate { return true; }
                    };
                }

                var options = new GrpcChannelOptions
                {
                    HttpHandler = handler
                };

                var channel = GrpcChannel.ForAddress($"https://{address}:{port}", options);

                // Assure qu'on peut établir une connexion
                await channel.ConnectAsync();

                Channel = channel;
                IsConnected = true;

                _logger?.LogInformation("Connexion gRPC sécurisée réussie à {Address}:{Port}", address, port);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Échec de connexion gRPC sécurisée à {Address}:{Port}", address, port);
                IsConnected = false;
                return false;
            }
        }
        // Version HTTPS avec certificat
        public async Task<bool> ConnectSecure(string address, int port, bool ignoreCertErrors = false)
        {
            try
            {
                // Chemin du certificat CA
                var caFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Certs/ca.crt");
                if (!File.Exists(caFilePath))
                {
                    _logger?.LogError("CA certificate file not found at: {Path}", caFilePath);
                    return false;
                }

                // Charger le certificat CA
                var caCert = new X509Certificate2(caFilePath);

                // Configuration du handler HTTP
                var handler = new SocketsHttpHandler
                {
                    PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
                    KeepAlivePingDelay = TimeSpan.FromSeconds(60),
                    KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
                    EnableMultipleHttp2Connections = true
                };

                if (ignoreCertErrors)
                {
                    handler.SslOptions = new System.Net.Security.SslClientAuthenticationOptions
                    {
                        RemoteCertificateValidationCallback = delegate { return true; },
                        EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13
                    };
                }
                else
                {
                    var chainPolicy = new X509ChainPolicy
                    {
                        RevocationMode = X509RevocationMode.NoCheck
                    };

                    chainPolicy.ExtraStore.Add(caCert); // ✅ Ajouter le certificat ici

                    handler.SslOptions = new System.Net.Security.SslClientAuthenticationOptions
                    {
                        CertificateChainPolicy = chainPolicy,
                        EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13
                    };
                }

                // Options du canal gRPC
                var options = new GrpcChannelOptions
                {
                    HttpHandler = handler,
                    MaxReceiveMessageSize = MAX_SIZE_GET_LOG,
                    MaxSendMessageSize = 10 * 1024 * 1024 // 10MB
                };

                _logger?.LogInformation("Tentative de connexion à https://{Address}:{Port}", address, port);
                var channel = GrpcChannel.ForAddress($"https://{address}:{port}", options);

                // Attente de connexion avec timeout
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await channel.ConnectAsync(cts.Token); // ✅ correcte ici

                Channel = channel;

                // Initialiser les clients
                ConnectClient = new connect.Connect.ConnectClient(channel);
                DeviceClient = new DeviceClient(channel);

                // Test de connexion (optionnel mais utile)
                try
                {
                    var request = new connect.GetDeviceListRequest();
                    var response = await ConnectClient.GetDeviceListAsync(request,
                        deadline: DateTime.UtcNow.AddSeconds(5));

                    _logger?.LogInformation("Connexion réussie. Nombre de périphériques : {Count}", response.DeviceInfos.Count);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning("Connexion établie mais l'appel de test a échoué : {Message}", ex.Message);
                }

                IsConnected = true;
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Échec de la connexion sécurisée à {Address}:{Port}: {Message}", address, port, ex.Message);
                IsConnected = false;
                return false;
            }
        }


        private void InitStubs(Channel channel)
        {
            ConnectClient = new connect.Connect.ConnectClient(channel);
            DeviceClient = new DeviceClient(channel);
        }

        public void Disconnect()
        {
            try
            {
                if (Channel != null)
                {
                    Channel.ShutdownAsync().Wait(TimeSpan.FromSeconds(5));
                    Channel.Dispose();
                    Channel = null;
                }

                if (_channel != null)
                {
                    _channel.ShutdownAsync().Wait(TimeSpan.FromSeconds(5));
                    _channel = null;
                }

                IsConnected = false;
                _logger?.LogInformation("Déconnexion gRPC réussie");
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Erreur pendant la déconnexion gRPC: {Message}", ex.Message);
            }
        }
    }
}