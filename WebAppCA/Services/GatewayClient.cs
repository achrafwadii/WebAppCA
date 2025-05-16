using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Grpcconnect;
using Grpcdevice;
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

        public Grpcconnect.Connect.ConnectClient ConnectClient { get; private set; }
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

        public async Task<bool> Connect(string address, int port)
        {
            try
            {
                // Activez la prise en charge de HTTP/2 non-sécurisé
                AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

                // Configuration pour le HTTP handler
                var handler = new SocketsHttpHandler
                {
                    EnableMultipleHttp2Connections = true,
                    KeepAlivePingDelay = TimeSpan.FromSeconds(60),
                    KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
                    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1)
                };

                var options = new GrpcChannelOptions
                {
                    HttpHandler = handler,
                    DisposeHttpClient = true
                };

                // Créer le canal avec les options appropriées
                var channel = GrpcChannel.ForAddress($"http://{address}:{port}", options);

                // Tenter de se connecter
                await channel.ConnectAsync();

                Channel = channel;
                IsConnected = true;

                _logger?.LogInformation("Connexion gRPC réussie à {Address}:{Port}", address, port);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Échec de connexion gRPC à {Address}:{Port}: {Message}", address, port, ex.Message);
                IsConnected = false;
                return false;
            }
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
                _logger?.LogError(ex, "Échec de connexion gRPC sécurisée à {Address}:{Port}: {Message}", address, port, ex.Message);
                IsConnected = false;
                return false;
            }
        }

        private void InitStubs(Channel channel)
        {
            ConnectClient = new Grpcconnect.Connect.ConnectClient(channel);
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