using connect;
using Grpc.Core;
using Grpc.Net.Client;
using Grpcdevice;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using static Grpcdevice.Device;

namespace WebAppCA.Services
{
    public class GatewayClient 
    {
        private readonly ILogger<GatewayClient> _logger;
        private readonly SemaphoreSlim _connectionSemaphore = new(1, 1);
        private readonly ConcurrentDictionary<string, GrpcChannel> _channelCache = new();
        private readonly Timer _cleanupTimer;

        private GrpcChannel _currentChannel;
        private volatile bool _isConnected;
        private volatile bool _disposed;
        private CancellationTokenSource _cancellationTokenSource;

        // Static HttpClient for connection pooling
        private static readonly HttpClient _sharedHttpClient = CreateOptimizedHttpClient();

        public bool IsConnected => _isConnected && !_disposed;
        public connect.Connect.ConnectClient ConnectClient { get; private set; }
        public DeviceClient DeviceClient { get; private set; }

        // Expose the current channel for other service clients
        public GrpcChannel CurrentChannel => _currentChannel;

        public event EventHandler<bool> ConnectionStatusChanged;

        public GatewayClient(ILogger<GatewayClient> logger = null)
        {
            _logger = logger;
            _cancellationTokenSource = new CancellationTokenSource();

            // Cleanup timer every 10 minutes
            _cleanupTimer = new Timer(CleanupUnusedChannels, null,
                TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));
        }

        private static HttpClient CreateOptimizedHttpClient()
        {
            var handler = new SocketsHttpHandler
            {
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(10),
                PooledConnectionLifetime = TimeSpan.FromMinutes(30),
                MaxConnectionsPerServer = 2,
                EnableMultipleHttp2Connections = true,
                KeepAlivePingDelay = TimeSpan.FromSeconds(30),
                KeepAlivePingTimeout = TimeSpan.FromSeconds(5)
            };

            return new HttpClient(handler);
        }

        #region Connection Methods

        public bool Connect(string serverAddr, int serverPort)
        {
            return ConnectAsync(serverAddr, serverPort).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public async Task<bool> ConnectSecure(string serverAddr, int serverPort, bool ignoreCertErrors = false)
        {
            return await ConnectSecureAsync(serverAddr, serverPort, ignoreCertErrors);
        }

        public bool Connect(string caFile, string serverAddr, int serverPort)
        {
            return ConnectWithCertificateAsync(caFile, serverAddr, serverPort)
                .ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public async Task<bool> ConnectAsync(string serverAddr, int serverPort)
        {
            return await ConnectInternalAsync($"http://{serverAddr}:{serverPort}", false);
        }

        public async Task<bool> ConnectSecureAsync(string serverAddr, int serverPort, bool ignoreCertErrors = false)
        {
            return await ConnectInternalAsync($"https://{serverAddr}:{serverPort}", true, ignoreCertErrors);
        }

        public async Task<bool> ConnectWithCertificateAsync(string caFile, string serverAddr, int serverPort)
        {
            var fullPath = Path.IsPathRooted(caFile) ? caFile : Path.Combine("Certs", caFile);
            return await ConnectInternalAsync($"https://{serverAddr}:{serverPort}", true, false, fullPath);
        }

        private async Task<bool> ConnectInternalAsync(string address, bool useHttps,
            bool ignoreCertErrors = false, string caCertPath = null)
        {
            if (_disposed) return false;

            var channelKey = $"{address}_{useHttps}_{ignoreCertErrors}_{caCertPath}";

            // Fast path: reuse existing connection
            if (_channelCache.TryGetValue(channelKey, out var existingChannel) &&
                existingChannel.State == ConnectivityState.Ready)
            {
                if (!await _connectionSemaphore.WaitAsync(100))
                {
                    _logger?.LogWarning("Connection semaphore timeout");
                    return false;
                }

                try
                {
                    _currentChannel = existingChannel;
                    InitializeClients(_currentChannel);
                    SetConnectionStatus(true);
                    return true;
                }
                finally
                {
                    _connectionSemaphore.Release();
                }
            }

            // Slow path: create new connection
            if (!await _connectionSemaphore.WaitAsync(5000, _cancellationTokenSource.Token))
            {
                _logger?.LogError("Failed to acquire connection semaphore within timeout");
                return false;
            }

            try
            {
                var channel = await CreateChannelAsync(address, useHttps, ignoreCertErrors, caCertPath);
                if (channel == null)
                    return false;

                // Quick connectivity test
                if (!await TestConnectivityFastAsync(channel))
                {
                    await DisposeChannelSafelyAsync(channel);
                    return false;
                }

                // Store and activate
                var oldChannel = _currentChannel;
                _currentChannel = channel;
                _channelCache.TryAdd(channelKey, channel);

                InitializeClients(channel);
                SetConnectionStatus(true);

                // Cleanup old channel asynchronously
                if (oldChannel != null)
                {
                    _ = Task.Run(() => DisposeChannelSafelyAsync(oldChannel));
                }

                _logger?.LogInformation("gRPC connection established to {Address}", address);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Connection failed to {Address}", address);
                SetConnectionStatus(false);
                return false;
            }
            finally
            {
                _connectionSemaphore.Release();
            }
        }

        private async Task<GrpcChannel> CreateChannelAsync(string address, bool useHttps,
            bool ignoreCertErrors, string caCertPath)
        {
            try
            {
                var options = new GrpcChannelOptions
                {
                    HttpClient = _sharedHttpClient,
                    MaxReceiveMessageSize = 50 * 1024 * 1024, // 50MB
                    MaxSendMessageSize = 10 * 1024 * 1024,    // 10MB
                    ThrowOperationCanceledOnCancellation = true,
                    DisposeHttpClient = false // Important: don't dispose shared client
                };

                if (useHttps)
                {
                    ConfigureHttpsOptions(options, ignoreCertErrors, caCertPath);
                }

                var channel = GrpcChannel.ForAddress(address, options);

                // Quick connection test with short timeout
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                await channel.ConnectAsync(cts.Token);

                return channel;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to create channel for {Address}", address);
                return null;
            }
        }

        private void ConfigureHttpsOptions(GrpcChannelOptions options, bool ignoreCertErrors, string caCertPath)
        {
            // Note: For HTTPS with shared HttpClient, certificate validation is handled
            // at the HttpClient level. This is a simplified approach.
            if (ignoreCertErrors)
            {
                _logger?.LogWarning("Certificate validation will be handled by shared HttpClient configuration");
            }
            else if (!string.IsNullOrEmpty(caCertPath) && File.Exists(caCertPath))
            {
                _logger?.LogInformation("Custom CA certificate handling requires HttpClient configuration");
            }
        }

        private async Task<bool> TestConnectivityFastAsync(GrpcChannel channel)
        {
            try
            {
                var connectClient = new connect.Connect.ConnectClient(channel);
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

                var request = new connect.GetDeviceListRequest();
                await connectClient.GetDeviceListAsync(request,
                    deadline: DateTime.UtcNow.AddSeconds(2),
                    cancellationToken: cts.Token);

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogDebug("Connectivity test failed: {Message}", ex.Message);
                return false;
            }
        }

        private void InitializeClients(GrpcChannel channel)
        {
            ConnectClient = new connect.Connect.ConnectClient(channel);
            DeviceClient = new DeviceClient(channel);
        }

        private void SetConnectionStatus(bool connected)
        {
            if (_isConnected != connected)
            {
                _isConnected = connected;
                ConnectionStatusChanged?.Invoke(this, connected);
            }
        }

        #endregion

        #region Health Check & Monitoring

        public async Task<bool> IsHealthyAsync()
        {
            if (!_isConnected || _currentChannel == null || _disposed)
                return false;

            try
            {
                var connectClient = new connect.Connect.ConnectClient(_currentChannel);
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

                await connectClient.GetDeviceListAsync(new connect.GetDeviceListRequest(),
                    deadline: DateTime.UtcNow.AddSeconds(2),
                    cancellationToken: cts.Token);
                return true;
            }
            catch
            {
                SetConnectionStatus(false);
                return false;
            }
        }

        #endregion

        #region Cleanup & Maintenance

        private void CleanupUnusedChannels(object state)
        {
            if (_disposed || _channelCache.Count < 5) return;

            var channelsToRemove = new List<string>();

            foreach (var kvp in _channelCache)
            {
                if (kvp.Value != _currentChannel &&
                    (kvp.Value.State == ConnectivityState.Shutdown ||
                     kvp.Value.State == ConnectivityState.TransientFailure))
                {
                    channelsToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in channelsToRemove)
            {
                if (_channelCache.TryRemove(key, out var channel))
                {
                    _ = Task.Run(() => DisposeChannelSafelyAsync(channel));
                }
            }

            if (channelsToRemove.Count > 0)
            {
                _logger?.LogDebug("Cleaned up {Count} unused channels", channelsToRemove.Count);
            }
        }

        private async Task DisposeChannelSafelyAsync(GrpcChannel channel)
        {
            if (channel == null) return;

            try
            {
                if (channel.State != ConnectivityState.Shutdown)
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    await channel.ShutdownAsync();
                }
                channel.Dispose();
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(ex, "Error disposing channel");
            }
        }

        public void Disconnect()
        {
            DisconnectAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public void Disconnect()
        {
            if (_disposed) return;

            await _connectionSemaphore.WaitAsync();
            try
            {
                SetConnectionStatus(false);

                if (_currentChannel != null)
                {
                    await DisposeChannelSafelyAsync(_currentChannel);
                    _currentChannel = null;
                }

                ConnectClient = null;
                DeviceClient = null;

                _logger?.LogInformation("gRPC disconnection completed");
            }
            finally
            {
                _connectionSemaphore.Release();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _cancellationTokenSource?.Cancel();
            _cleanupTimer?.Dispose();

            // Cleanup all channels asynchronously
            var tasks = new List<Task>();
            foreach (var channel in _channelCache.Values)
            {
                tasks.Add(DisposeChannelSafelyAsync(channel));
            }

            try
            {
                Task.WaitAll(tasks.ToArray(), TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error during disposal cleanup");
            }
        }
    }
}