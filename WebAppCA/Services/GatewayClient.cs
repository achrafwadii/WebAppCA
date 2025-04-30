using Grpc.Net.Client;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;

namespace WebAppCA.Services
{
    public class GatewayClient
    {
        private GrpcChannel _channel;
        public GrpcChannel Channel => _channel;

        public void Connect(string caCertPath, string address, int port)
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            // Configure SSL if using a CA certificate
            if (File.Exists(caCertPath))
            {
                var caCert = new X509Certificate2(caCertPath);
                handler.ClientCertificates.Add(caCert);
            }

            var httpClient = new HttpClient(handler);
            _channel = GrpcChannel.ForAddress($"https://{address}:{port}", new GrpcChannelOptions
            {
                HttpClient = httpClient
            });
        }

        public void Close()
        {
            _channel?.Dispose();
            _channel = null;
        }
    }
}