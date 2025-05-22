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
    public class ConnectSvc : IDisposable
    {
        private readonly ConnectClient _client;
        private readonly ILogger<ConnectSvc> _logger;

        // Expose the client for other services that need direct access
        public ConnectClient Client => _isConnected ? _client : null;
        private readonly SemaphoreSlim _operationSemaphore = new(10, 10); // Increased concurrency
        private readonly ConcurrentDictionary<string, object> _operationCache = new();
        private readonly Timer _cacheCleanupTimer;

        private volatile bool _isConnected;
        private volatile bool _disposed;
        private CancellationTokenSource _cancellationTokenSource;

        public bool IsConnected => _isConnected && !_disposed;
        public event EventHandler<bool> ConnectionStatusChanged;

        public ConnectSvc(ConnectClient client, ILogger<ConnectSvc> logger)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cancellationTokenSource = new CancellationTokenSource();

            // Cache cleanup every 5 minutes
            _cacheCleanupTimer = new Timer(CleanupCache, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));

            // Quick connection test without blocking
            _ = Task.Run(TestConnectionAsync);
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
        }

        private void CleanupCache(object state)
        {
            if (_operationCache.Count > 1000) // Only cleanup if cache is large
            {
                _operationCache.Clear();
            }
        }

        public async Task<bool> TryReconnectAsync()
        {
            if (_disposed || _isConnected) return _isConnected;

            try
            {
                await TestConnectionAsync();
                return _isConnected;
            }
            catch
            {
                return false;
            }
        }

        private async Task<T> ExecuteOperationAsync<T>(
            Func<ConnectClient, CallOptions, Task<T>> operation,
            int timeoutMs = 10000,
            T defaultValue = default(T),
            string cacheKey = null)
        {
            if (_disposed) return defaultValue;

            // Check cache first
            if (!string.IsNullOrEmpty(cacheKey) &&
                _operationCache.TryGetValue(cacheKey, out var cachedResult) &&
                cachedResult is T cached)
            {
                return cached;
            }

            if (!await _operationSemaphore.WaitAsync(100, _cancellationTokenSource.Token))
            {
                _logger.LogWarning("Operation timeout - too many concurrent requests");
                return defaultValue;
            }

            try
            {
                if (!_isConnected)
                {
                    _logger.LogWarning("Client not connected, returning default value");
                    return defaultValue;
                }

                var callOptions = new CallOptions(
                    deadline: DateTime.UtcNow.AddMilliseconds(timeoutMs),
                    cancellationToken: _cancellationTokenSource.Token
                );

                var result = await operation(_client, callOptions);

                // Cache successful results
                if (!string.IsNullOrEmpty(cacheKey) && result != null)
                {
                    _operationCache.TryAdd(cacheKey, result);
                }

                return result;
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable ||
                                         ex.StatusCode == StatusCode.DeadlineExceeded ||
                                         ex.StatusCode == StatusCode.Cancelled)
            {
                SetConnectionStatus(false);
                _logger.LogWarning("RPC operation failed: {Status}", ex.Status);
                return defaultValue;
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Operation cancelled");
                return defaultValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during operation");
                return defaultValue;
            }
            finally
            {
                _operationSemaphore.Release();
            }
        }

        // Optimized public methods
        public async Task<RepeatedField<DeviceInfo>> GetDeviceListAsync()
        {
            var cacheKey = $"devicelist_{DateTime.UtcNow.Minute}"; // Cache for 1 minute
            return await ExecuteOperationAsync(
                async (client, opts) =>
                {
                    var request = new GetDeviceListRequest();
                    var response = await client.GetDeviceListAsync(request, opts);
                    return response.DeviceInfos;
                },
                timeoutMs: 5000,
                defaultValue: new RepeatedField<DeviceInfo>(),
                cacheKey: cacheKey
            );
        }

        public async Task<uint> ConnectAsync(ConnectInfo connectInfo)
        {
            return await ExecuteOperationAsync(
                async (client, opts) =>
                {
                    var request = new ConnectRequest { ConnectInfo = connectInfo };
                    var response = await client.ConnectAsync(request, opts);
                    _logger.LogInformation("Device connected with ID: {DeviceID}", response.DeviceID);
                    return response.DeviceID;
                },
                timeoutMs: 8000,
                defaultValue: 0u
            );
        }

        public async Task<RepeatedField<SearchDeviceInfo>> SearchDeviceAsync(int timeoutMs = 5000)
        {
            return await ExecuteOperationAsync(
                async (client, opts) =>
                {
                    var request = new SearchDeviceRequest { Timeout = (uint)timeoutMs };
                    var response = await client.SearchDeviceAsync(request, opts);
                    return response.DeviceInfos;
                },
                timeoutMs: timeoutMs + 1000,
                defaultValue: new RepeatedField<SearchDeviceInfo>()
            );
        }

        public async Task DisconnectAsync(uint[] deviceIDs)
        {
            if (deviceIDs?.Length == 0) return;

            await ExecuteOperationAsync(
                async (client, opts) =>
                {
                    var request = new DisconnectRequest();
                    request.DeviceIDs.AddRange(deviceIDs);
                    await client.DisconnectAsync(request, opts);
                    return true;
                },
                timeoutMs: 5000
            );
        }

        public async Task DisconnectAllAsync()
        {
            await ExecuteOperationAsync(
                async (client, opts) =>
                {
                    await client.DisconnectAllAsync(new DisconnectAllRequest(), opts);
                    return true;
                },
                timeoutMs: 5000
            );
        }

        public async Task<uint> ConnectDeviceAsync(string ip, int port, bool useSSL = false)
        {
            var info = new ConnectInfo
            {
                IPAddr = ip,
                Port = port,
                UseSSL = useSSL
            };
            return await ConnectAsync(info);
        }

        // Synchronous methods for backward compatibility (non-blocking)
        public RepeatedField<DeviceInfo> GetDeviceList()
        {
            var task = GetDeviceListAsync();
            if (task.Wait(3000)) // 3 second timeout
            {
                return task.Result;
            }
            _logger.LogWarning("GetDeviceList timed out, returning empty list");
            return new RepeatedField<DeviceInfo>();
        }

        public uint Connect(ConnectInfo connectInfo)
        {
            var task = ConnectAsync(connectInfo);
            if (task.Wait(5000)) // 5 second timeout
            {
                return task.Result;
            }
            _logger.LogWarning("Connect operation timed out");
            return 0u;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _cancellationTokenSource?.Cancel();
            _cacheCleanupTimer?.Dispose();

            // Don't wait for semaphore release in dispose
            _operationSemaphore?.Dispose();
            _cancellationTokenSource?.Dispose();
            _operationCache?.Clear();
        }
    }
}