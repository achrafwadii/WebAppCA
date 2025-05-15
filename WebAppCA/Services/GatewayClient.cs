using System;
using System.IO;
using System.Collections.Generic;
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
                var channel = GrpcChannel.ForAddress($"http://{address}:{port}");

                // Assure qu'on peut établir une connexion
                await channel.ConnectAsync();

                Channel = channel;
                IsConnected = true;

                _logger.LogInformation("Connexion gRPC réussie à {Address}:{Port}", address, port);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Échec de connexion gRPC à {Address}:{Port}", address, port);
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
            _channel?.ShutdownAsync().Wait();
        }
    }
}