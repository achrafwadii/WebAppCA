using Connect;
using Grpc.Core;
using Google.Protobuf.Collections;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;

namespace WebAppCA.Services
{
    public class ConnectSvc
    {
        private const int SEARCH_TIMEOUT_MS = 5000;
        private readonly ILogger<ConnectSvc> _logger;
        private Connect.Connect.ConnectClient _connectClient;

        // Constructor for Grpc.Core.Channel (legacy)
        public ConnectSvc(Channel channel, ILogger<ConnectSvc> logger = null)
        {
            _logger = logger;
            if (channel != null)
            {
                _connectClient = new Connect.Connect.ConnectClient(channel);
                _logger?.LogInformation("ConnectSvc initialisé avec Grpc.Core.Channel");
            }
            else
            {
                _logger?.LogWarning("ConnectSvc initialisé avec un channel null");
            }
        }

        // Constructor for Grpc.Net.Client.GrpcChannel (newer)
        public ConnectSvc(GrpcChannel channel, ILogger<ConnectSvc> logger = null)
        {
            _logger = logger;
            if (channel != null)
            {
                _connectClient = new Connect.Connect.ConnectClient(channel);
                _logger?.LogInformation("ConnectSvc initialisé avec GrpcChannel");
            }
            else
            {
                _logger?.LogWarning("ConnectSvc initialisé avec un GrpcChannel null");
            }
        }

        public ConnectSvc(ILogger<ConnectSvc> logger = null)
        {
            _logger = logger;
            _logger?.LogWarning("ConnectSvc initialisé sans channel (aucune connexion disponible)");
        }
        public ConnectSvc()
        {
            // Cette implémentation sera utilisée en mode fallback quand le canal gRPC n'est pas disponible
            // Ne pas lancer d'exceptions, mais logger ou retourner des valeurs par défaut
        }

        private void EnsureConnectClient()
        {
            if (_connectClient == null)
            {
                var errorMessage = "Le client gRPC n'est pas initialisé. Assurez-vous que le canal (channel) est correctement configuré.";
                _logger?.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }
        }

        public RepeatedField<Connect.DeviceInfo> GetDeviceList()
        {
            EnsureConnectClient();

            try
            {
                var request = new GetDeviceListRequest { };
                var response = _connectClient.GetDeviceList(request);
                return response.DeviceInfos;
            }
            catch (RpcException ex)
            {
                _logger?.LogError(ex, "Erreur lors de l'appel à GetDeviceList: {StatusCode} - {Message}", ex.StatusCode, ex.Message);
                throw;
            }
        }

        public RepeatedField<Connect.SearchDeviceInfo> SearchDevice()
        {
            EnsureConnectClient();

            try
            {
                var request = new SearchDeviceRequest { Timeout = SEARCH_TIMEOUT_MS };
                var response = _connectClient.SearchDevice(request);
                return response.DeviceInfos;
            }
            catch (RpcException ex)
            {
                _logger?.LogError(ex, "Erreur lors de l'appel à SearchDevice: {StatusCode} - {Message}", ex.StatusCode, ex.Message);
                throw;
            }
        }

        public uint Connect(Connect.ConnectInfo connectInfo)
        {
            EnsureConnectClient();

            try
            {
                _logger?.LogInformation("Tentative de connexion à {IPAddr}:{Port}", connectInfo.IPAddr, connectInfo.Port);
                var request = new ConnectRequest { ConnectInfo = connectInfo };
                var response = _connectClient.Connect(request);
                _logger?.LogInformation("Connexion réussie. DeviceID: {DeviceID}", response.DeviceID);
                return response.DeviceID;
            }
            catch (RpcException ex)
            {
                _logger?.LogError(ex, "Erreur lors de l'appel à Connect pour {IPAddr}:{Port}: {StatusCode} - {Message}",
                    connectInfo.IPAddr, connectInfo.Port, ex.StatusCode, ex.Message);
                throw;
            }
        }

        public void Disconnect(uint[] deviceIDs)
        {
            EnsureConnectClient();

            try
            {
                var request = new DisconnectRequest { };
                request.DeviceIDs.AddRange(deviceIDs);
                _connectClient.Disconnect(request);
            }
            catch (RpcException ex)
            {
                _logger?.LogError(ex, "Erreur lors de l'appel à Disconnect: {StatusCode} - {Message}", ex.StatusCode, ex.Message);
                throw;
            }
        }

        public void DisconnectAll()
        {
            EnsureConnectClient();

            try
            {
                var request = new DisconnectAllRequest { };
                _connectClient.DisconnectAll(request);
            }
            catch (RpcException ex)
            {
                _logger?.LogError(ex, "Erreur lors de l'appel à DisconnectAll: {StatusCode} - {Message}", ex.StatusCode, ex.Message);
                throw;
            }
        }

        public void AddAsyncConnection(AsyncConnectInfo[] asyncConns)
        {
            EnsureConnectClient();

            try
            {
                var request = new AddAsyncConnectionRequest { };
                request.ConnectInfos.AddRange(asyncConns);
                _connectClient.AddAsyncConnection(request);
            }
            catch (RpcException ex)
            {
                _logger?.LogError(ex, "Erreur lors de l'appel à AddAsyncConnection: {StatusCode} - {Message}", ex.StatusCode, ex.Message);
                throw;
            }
        }

        public void DeleteAsyncConnection(uint[] deviceIDs)
        {
            EnsureConnectClient();

            try
            {
                var request = new DeleteAsyncConnectionRequest { };
                request.DeviceIDs.AddRange(deviceIDs);
                _connectClient.DeleteAsyncConnection(request);
            }
            catch (RpcException ex)
            {
                _logger?.LogError(ex, "Erreur lors de l'appel à DeleteAsyncConnection: {StatusCode} - {Message}", ex.StatusCode, ex.Message);
                throw;
            }
        }

        public RepeatedField<Connect.PendingDeviceInfo> GetPendingList()
        {
            EnsureConnectClient();

            try
            {
                var request = new GetPendingListRequest { };
                var response = _connectClient.GetPendingList(request);
                return response.DeviceInfos;
            }
            catch (RpcException ex)
            {
                _logger?.LogError(ex, "Erreur lors de l'appel à GetPendingList: {StatusCode} - {Message}", ex.StatusCode, ex.Message);
                throw;
            }
        }

        public AcceptFilter GetAcceptFilter()
        {
            EnsureConnectClient();

            try
            {
                var request = new GetAcceptFilterRequest { };
                var response = _connectClient.GetAcceptFilter(request);
                return response.Filter;
            }
            catch (RpcException ex)
            {
                _logger?.LogError(ex, "Erreur lors de l'appel à GetAcceptFilter: {StatusCode} - {Message}", ex.StatusCode, ex.Message);
                throw;
            }
        }

        public void SetAcceptFilter(AcceptFilter filter)
        {
            EnsureConnectClient();

            try
            {
                var request = new SetAcceptFilterRequest { Filter = filter };
                _connectClient.SetAcceptFilter(request);
            }
            catch (RpcException ex)
            {
                _logger?.LogError(ex, "Erreur lors de l'appel à SetAcceptFilter: {StatusCode} - {Message}", ex.StatusCode, ex.Message);
                throw;
            }
        }

        public void SetConnectionMode(uint[] deviceIDs, ConnectionMode mode)
        {
            EnsureConnectClient();

            try
            {
                var request = new SetConnectionModeMultiRequest { ConnectionMode = mode };
                request.DeviceIDs.AddRange(deviceIDs);
                _connectClient.SetConnectionModeMulti(request);
            }
            catch (RpcException ex)
            {
                _logger?.LogError(ex, "Erreur lors de l'appel à SetConnectionMode: {StatusCode} - {Message}", ex.StatusCode, ex.Message);
                throw;
            }
        }

        public void EnableSSL(uint[] deviceIDs)
        {
            EnsureConnectClient();

            try
            {
                var request = new EnableSSLMultiRequest { };
                request.DeviceIDs.AddRange(deviceIDs);
                _connectClient.EnableSSLMulti(request);
            }
            catch (RpcException ex)
            {
                _logger?.LogError(ex, "Erreur lors de l'appel à EnableSSL: {StatusCode} - {Message}", ex.StatusCode, ex.Message);
                throw;
            }
        }

        public void DisableSSL(uint[] deviceIDs)
        {
            EnsureConnectClient();

            try
            {
                var request = new DisableSSLMultiRequest { };
                request.DeviceIDs.AddRange(deviceIDs);
                _connectClient.DisableSSLMulti(request);
            }
            catch (RpcException ex)
            {
                _logger?.LogError(ex, "Erreur lors de l'appel à DisableSSL: {StatusCode} - {Message}", ex.StatusCode, ex.Message);
                throw;
            }
        }

        public IAsyncStreamReader<StatusChange> Subscribe(int queueSize)
        {
            EnsureConnectClient();

            try
            {
                var request = new SubscribeStatusRequest { QueueSize = queueSize };
                var streamCall = _connectClient.SubscribeStatus(request);
                return streamCall.ResponseStream;
            }
            catch (RpcException ex)
            {
                _logger?.LogError(ex, "Erreur lors de l'appel à Subscribe: {StatusCode} - {Message}", ex.StatusCode, ex.Message);
                throw;
            }
        }
    }
}