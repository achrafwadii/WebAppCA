using System;
using System.Threading.Tasks;
using Grpc.Core;
using Google.Protobuf.Collections;
using Microsoft.Extensions.Logging;
using connect;
using static connect.Connect;
using Grpc.Net.Client;
using System.Net.Http;

namespace WebAppCA.Services
{
    public class ConnectSvc
    {
        private const string GATEWAY_CA_FILE = "Certs/ca.crt";
        private const string GATEWAY_ADDR = "192.168.0.2";
        private const int GATEWAY_PORT = 4000;
        private const int SEARCH_TIMEOUT_MS = 5000;
        private readonly ILogger<ConnectSvc> _logger;
        private readonly ConnectClient _client;
        private bool _isConnected;
        private DateTime _lastAttempt;
        private static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(5);

        public bool IsConnected => _isConnected;
        public ChannelBase Channel { get; }

        public ConnectSvc(ConnectClient client, ILogger<ConnectSvc> logger = null)
        {
            _logger = logger;
            _client = client;
            TestInitialConnection();
        }

        private void TestInitialConnection()
        {
            try
            {
                var resp = _client.GetDeviceList(new GetDeviceListRequest());
                _isConnected = true;
                _logger?.LogInformation("Init ok. Devices: {0}", resp.DeviceInfos.Count);
            }
            catch (RpcException ex)
            {
                _isConnected = false;
                _logger?.LogError(ex, "Init failed: {0}", ex.Status);
            }
        }

        public async Task<bool> TryReconnectAsync()
        {
            if (_isConnected) return true;
            if (DateTime.Now - _lastAttempt < Cooldown) return false;

            _lastAttempt = DateTime.Now;

            try
            {
                var resp = await _client.GetDeviceListAsync(new GetDeviceListRequest());
                _isConnected = true;
                _logger?.LogInformation("Reconnected. Devices: {0}", resp.DeviceInfos.Count);
                return true;
            }
            catch (Exception ex)
            {
                _isConnected = false;
                _logger?.LogError(ex, "Reconnect failed: {0}", ex.Message);
                return false;
            }
        }
        public void Initialize(Channel channel)
        {
            try
            {
                if (channel == null)
                {
                    _logger.LogError("Channel is null during initialization");
                    throw new ArgumentNullException(nameof(channel));
                }
                var _client = new ConnectClient(channel);
                _client = new Connect.ConnectClient(channel);
                _logger.LogInformation("ConnectClient initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize ConnectClient");
                throw;
            }
        }
        private bool EnsureConnected()
        {
            if (!_isConnected)
            {
                _logger?.LogError("gRPC not connected");
                return false;
            }
            return true;
        }

        public RepeatedField<DeviceInfo> GetDeviceList()
        {
            if (!EnsureConnected()) return new RepeatedField<DeviceInfo>();

            try
            {
                var opts = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(5));
                var resp = _client.GetDeviceList(new GetDeviceListRequest(), opts);
                return resp.DeviceInfos;
            }
            catch (RpcException ex)
            {
                _isConnected = false;
                _logger?.LogError(ex, "GetDeviceList error: {0}", ex.Status);
                return new RepeatedField<DeviceInfo>();
            }
        }

        public uint Connect(ConnectInfo connectInfo)
        {
            if (!EnsureConnected()) return 0;

            try
            {
                var request = new ConnectRequest { ConnectInfo = connectInfo };
                var opts = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(10));
                var response = _client.Connect(request, opts);

                _logger?.LogInformation("Successfully connected to device with ID: {DeviceID}", response.DeviceID);
                return response.DeviceID;
            }
            catch (RpcException ex)
            {
                _isConnected = false;
                _logger?.LogError(ex, "Connect error: {Status} - {Message}", ex.Status, ex.Message);
                return 0;
            }
            catch (Exception ex)
            {
                _isConnected = false;
                _logger?.LogError(ex, "Unexpected error in Connect: {Message}", ex.Message);
                return 0;
            }
        }

        public RepeatedField<SearchDeviceInfo> SearchDevice()
        {
            if (!EnsureConnected()) return new RepeatedField<SearchDeviceInfo>();

            try
            {
                var request = new SearchDeviceRequest { Timeout = SEARCH_TIMEOUT_MS };
                var opts = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(7));
                var response = _client.SearchDevice(request, opts);
                return response.DeviceInfos;
            }
            catch (RpcException ex)
            {
                _isConnected = false;
                _logger?.LogError(ex, "SearchDevice error: {0}", ex.Status);
                return new RepeatedField<SearchDeviceInfo>();
            }
        }

        public void Disconnect(uint[] deviceIDs)
        {
            if (!EnsureConnected()) return;

            try
            {
                var request = new DisconnectRequest();
                request.DeviceIDs.AddRange(deviceIDs);
                _client.Disconnect(request);
            }
            catch (RpcException ex)
            {
                _isConnected = false;
                _logger?.LogError(ex, "Disconnect error: {0}", ex.Status);
            }
        }

        public void DisconnectAll()
        {
            if (!EnsureConnected()) return;

            try
            {
                _client.DisconnectAll(new DisconnectAllRequest());
            }
            catch (RpcException ex)
            {
                _isConnected = false;
                _logger?.LogError(ex, "DisconnectAll error: {0}", ex.Status);
            }
        }

        public void AddAsyncConnection(AsyncConnectInfo[] asyncConns)
        {
            if (!EnsureConnected()) return;

            try
            {
                var request = new AddAsyncConnectionRequest();
                request.ConnectInfos.AddRange(asyncConns);
                _client.AddAsyncConnection(request);
            }
            catch (RpcException ex)
            {
                _isConnected = false;
                _logger?.LogError(ex, "AddAsyncConnection error: {0}", ex.Status);
            }
        }

        public void DeleteAsyncConnection(uint[] deviceIDs)
        {
            if (!EnsureConnected()) return;

            try
            {
                var request = new DeleteAsyncConnectionRequest();
                request.DeviceIDs.AddRange(deviceIDs);
                _client.DeleteAsyncConnection(request);
            }
            catch (RpcException ex)
            {
                _isConnected = false;
                _logger?.LogError(ex, "DeleteAsyncConnection error: {0}", ex.Status);
            }
        }

        public RepeatedField<PendingDeviceInfo> GetPendingList()
        {
            if (!EnsureConnected()) return new RepeatedField<PendingDeviceInfo>();

            try
            {
                var request = new GetPendingListRequest();
                var response = _client.GetPendingList(request);
                return response.DeviceInfos;
            }
            catch (RpcException ex)
            {
                _isConnected = false;
                _logger?.LogError(ex, "GetPendingList error: {0}", ex.Status);
                return new RepeatedField<PendingDeviceInfo>();
            }
        }

        public AcceptFilter GetAcceptFilter()
        {
            if (!EnsureConnected()) return null;

            try
            {
                var request = new GetAcceptFilterRequest();
                var response = _client.GetAcceptFilter(request);
                return response.Filter;
            }
            catch (RpcException ex)
            {
                _isConnected = false;
                _logger?.LogError(ex, "GetAcceptFilter error: {0}", ex.Status);
                return null;
            }
        }

       
        public void SetAcceptFilter(AcceptFilter filter)
        {
            if (!EnsureConnected()) return;

            try
            {
                var request = new SetAcceptFilterRequest { Filter = filter };
                _client.SetAcceptFilter(request);
            }
            catch (RpcException ex)
            {
                _isConnected = false;
                _logger?.LogError(ex, "SetAcceptFilter error: {0}", ex.Status);
            }
        }

        public void SetConnectionMode(uint[] deviceIDs, ConnectionMode mode)
        {
            if (!EnsureConnected()) return;

            try
            {
                var request = new SetConnectionModeMultiRequest { ConnectionMode = mode };
                request.DeviceIDs.AddRange(deviceIDs);
                _client.SetConnectionModeMulti(request);
            }
            catch (RpcException ex)
            {
                _isConnected = false;
                _logger?.LogError(ex, "SetConnectionMode error: {0}", ex.Status);
            }
        }

        public void EnableSSL(uint[] deviceIDs)
        {
            if (!EnsureConnected()) return;

            try
            {
                var request = new EnableSSLMultiRequest();
                request.DeviceIDs.AddRange(deviceIDs);
                _client.EnableSSLMulti(request);
            }
            catch (RpcException ex)
            {
                _isConnected = false;
                _logger?.LogError(ex, "EnableSSL error: {0}", ex.Status);
            }
        }

        public void DisableSSL(uint[] deviceIDs)
        {
            if (!EnsureConnected()) return;

            try
            {
                var request = new DisableSSLMultiRequest();
                request.DeviceIDs.AddRange(deviceIDs);
                _client.DisableSSLMulti(request);
            }
            catch (RpcException ex)
            {
                _isConnected = false;
                _logger?.LogError(ex, "DisableSSL error: {0}", ex.Status);
            }
        }

        public IAsyncStreamReader<StatusChange> Subscribe(int queueSize)
        {
            if (!EnsureConnected()) return null;

            try
            {
                var request = new SubscribeStatusRequest { QueueSize = queueSize };
                var streamCall = _client.SubscribeStatus(request);
                return streamCall.ResponseStream;
            }
            catch (RpcException ex)
            {
                _isConnected = false;
                _logger?.LogError(ex, "Subscribe error: {0}", ex.Status);
                return null;
            }
        }

        public uint ConnectDevice(string ip, int port)
        {
            var info = new ConnectInfo { IPAddr = ip, Port = port, UseSSL = true };
            return Connect(info);
        }
    }
}