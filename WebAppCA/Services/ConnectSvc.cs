// WebAppCA/Services/ConnectSvc.cs - Version améliorée
using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Google.Protobuf.Collections;
using Microsoft.Extensions.Logging;
using connect;
using static connect.Connect;
using System.Collections.Concurrent;

namespace WebAppCA.Services
{
    public class ConnectSvc : connect.Connect.ConnectClient
    {
        private readonly ConnectClient _client;
        private bool _isConnected;
        private DateTime _lastAttempt;
        private static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(5);

        public bool IsConnected => _isConnected;
        public ChannelBase Channel { get; }

        private volatile bool _isConnected;
        private volatile bool _disposed;
        private CancellationTokenSource _cancellationTokenSource;

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

        public ConnectSvc(ConnectClient client, ILogger<ConnectSvc> logger)
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

        private async Task TestConnectionAsync()
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                var request = new GetDeviceListRequest();
                var callOptions = new CallOptions(
                    deadline: DateTime.UtcNow.AddSeconds(2),
                    cancellationToken: cts.Token
                );

                await _client.GetDeviceListAsync(request, callOptions);
                SetConnectionStatus(true);
            }
            catch
            {
                SetConnectionStatus(false);
            }
        }

        private void SetConnectionStatus(bool connected)
        {
            if (_isConnected != connected)
            {
                _isConnected = connected;
                ConnectionStatusChanged?.Invoke(this, connected);
            }
            return true;
        }

        private void CleanupCache(object state)
        {
            try
            {
                _operationCache.Clear();
            }
            catch (RpcException ex)
            {
                _isConnected = false;
                _logger?.LogError(ex, "GetDeviceList error: {0}", ex.Status);
                return new RepeatedField<DeviceInfo>();
            }
        }

        public async Task<bool> TryReconnectAsync()
        {
            try
            {
                await TestConnectionAsync();
                return _isConnected;
            }
            catch
            {
                _isConnected = false;
                _logger?.LogError(ex, "Connect error: {Status}", ex.Status);
                return 0;
            }
        }

        private async Task<T> ExecuteOperationAsync<T>(
            Func<ConnectClient, CallOptions, Task<T>> operation,
            int timeoutMs = 10000,
            T defaultValue = default(T),
            string cacheKey = null)
        {
            try
            {
                var request = new SearchDeviceRequest { Timeout = SEARCH_TIMEOUT_MS };
                var response = _client.SearchDevice(request);
                return response.DeviceInfos;
            }
            catch (RpcException ex)
            {
                _isConnected = false;
                _logger?.LogError(ex, "SearchDevice error: {0}", ex.Status);
                return new RepeatedField<SearchDeviceInfo>();
            }

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
            try
            {
                var request = new DisconnectAllRequest();
                _client.DisconnectAll(request);
            }
            catch (RpcException ex)
            {
                _isConnected = false;
                _logger?.LogError(ex, "DisconnectAll error: {0}", ex.Status);
            }
        }

        public void AddAsyncConnection(AsyncConnectInfo[] asyncConns)
        {
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