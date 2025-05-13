// WebAppCA/Services/GatewayClient.cs
using System;
using System.IO;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Connect;
using Device;
using static Device.Device;

namespace WebAppCA.Services
{
    public class GatewayClient
    {
        readonly ILogger<GatewayClient> _logger;
        Channel _coreChannel;
        GrpcChannel _netChannel;
        // GatewayClient.cs (ajouter la propriété)*
        public ChannelBase Channel => _coreChannel;

        public bool IsConnected => _coreChannel != null && _coreChannel.State == ChannelState.Ready;

        public Connect.Connect.ConnectClient ConnectClient { get; private set; }
        public DeviceClient DeviceClient { get; private set; }

        public GatewayClient(ILogger<GatewayClient> logger = null)
        {
            _logger = logger;
        }
        // GatewayClient.cs
        // Remplacer la seconde surcharge par celle-ci
        public bool Connect(string caPath, string certPath, string keyPath, string address, int port)
        {
            try
            {
                var cacert = File.ReadAllText(caPath);
                var clientCert = File.ReadAllText(certPath);
                var clientKey = File.ReadAllText(keyPath);

                var ssl = new SslCredentials(cacert, new KeyCertificatePair(clientCert, clientKey));
                _coreChannel = new Channel(address, port, ssl);
                _netChannel = GrpcChannel.ForAddress($"https://{address}:{port}", new GrpcChannelOptions { Credentials = ssl });

                InitStubs(_coreChannel);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Mode développement : connexion insecure
        public bool Connect(string address, int port)
        {
            try
            {
                _coreChannel = new Channel(address, port, ChannelCredentials.Insecure);
                InitStubs(_coreChannel);
                _logger?.LogInformation("gRPC insecure connected to {0}:{1}", address, port);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Insecure connect failed: {0}", ex.Message);
                return false;
            }
        }

        

        void InitStubs(Channel channel)
        {
            ConnectClient = new Connect.Connect.ConnectClient(channel);
            DeviceClient = new DeviceClient(channel);
        }

        public void Disconnect()
        {
            _coreChannel?.ShutdownAsync().Wait();
            _netChannel?.Dispose();
        }
    }
}
