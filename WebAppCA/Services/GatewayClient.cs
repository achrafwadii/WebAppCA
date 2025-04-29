using Grpc.Net.Client;
using System.Net.Http;

namespace WebAppCA.Services
{
    public class GatewayClient
    {
        private GrpcChannel _channel;
        public GrpcChannel Channel => _channel;

        public void Connect(string address, int port)
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            var httpClient = new HttpClient(handler);
            _channel = GrpcChannel.ForAddress($"https://{address}:{port}", new GrpcChannelOptions
            {
                HttpClient = httpClient
            });
        }
    }
}
