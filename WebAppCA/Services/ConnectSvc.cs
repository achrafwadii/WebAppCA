// ConnectSvc.cs
using System;
using System.Threading.Tasks;
using Grpc.Core;
using Google.Protobuf.Collections;
using Microsoft.Extensions.Logging;
using Connect;
using static Connect.Connect;

namespace WebAppCA.Services
{
    public class ConnectSvc
    {
        private readonly ILogger<ConnectSvc> _logger;
        private readonly ConnectClient _client;
        private bool _isConnected;
        private DateTime _lastAttempt;
        private static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(5);

        public ChannelBase Channel { get; }

        public ConnectSvc(ChannelBase channel, ILogger<ConnectSvc> logger = null)
        {
            _logger = logger;
            Channel = channel;
            _client = new ConnectClient(channel);
            TestInitialConnection();
        }
        public uint Connect(ConnectInfo info)
        {
            if (!EnsureConnected())
                throw new InvalidOperationException("Client gRPC non initialisé");

            return _client.Connect(new ConnectRequest { ConnectInfo = info }).DeviceID;
        }
        void TestInitialConnection()
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

        public bool IsConnected => _isConnected;

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

        bool EnsureConnected()
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

        public uint ConnectDevice(string ip, int port)
        {
            var info = new ConnectInfo { IPAddr = ip, Port = port, UseSSL = true };
            var resp = _client.Connect(new ConnectRequest { ConnectInfo = info });
            return resp.DeviceID;
        }

        public RepeatedField<SearchDeviceInfo> SearchDevice()
        {
            if (!EnsureConnected()) return new RepeatedField<SearchDeviceInfo>();

            try
            {
                var req = new SearchDeviceRequest { Timeout = 5000 };
                var opts = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(7));
                var resp = _client.SearchDevice(req, opts);
                return resp.DeviceInfos;
            }
            catch (RpcException ex)
            {
                _isConnected = false;
                _logger?.LogError(ex, "SearchDevice error: {0}", ex.Status);
                return new RepeatedField<SearchDeviceInfo>();
            }
        }

        public void Disconnect(uint[] ids)
        {
            _client.Disconnect(new DisconnectRequest { DeviceIDs = { ids } });
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
    }
}
