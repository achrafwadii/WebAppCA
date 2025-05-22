using Door;
using Grpc.Core;
using Google.Protobuf.Collections;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WebAppCA.Models;
using WebAppCA.Services;

namespace WebAppCA.Services
{
    public class DoorService
    {
        private readonly ILogger<DoorService> _logger;
        private readonly GatewayClient _gatewayClient;
        private readonly ConcurrentDictionary<uint, Door.Door.DoorClient> _doorClients = new();
        private readonly SemaphoreSlim _clientSemaphore = new(1, 1);
        private readonly Timer _cacheCleanupTimer;

        // Cache pour éviter les appels répétés
        private readonly ConcurrentDictionary<string, (DateTime Expiry, object Data)> _cache = new();
        private const int CACHE_DURATION_SECONDS = 30;

        public DoorService(ILogger<DoorService> logger, GatewayClient gatewayClient)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _gatewayClient = gatewayClient ?? throw new ArgumentNullException(nameof(gatewayClient));

            // Nettoyage du cache toutes les 2 minutes
            _cacheCleanupTimer = new Timer(CleanupCache, null,
                TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(2));
        }

        private void CleanupCache(object state)
        {
            var now = DateTime.UtcNow;
            var keysToRemove = _cache
                .Where(kvp => kvp.Value.Expiry < now)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keysToRemove)
            {
                _cache.TryRemove(key, out _);
            }

            if (keysToRemove.Count > 0)
            {
                _logger.LogDebug("Cleaned up {Count} expired cache entries", keysToRemove.Count);
            }
        }

        private async Task<Door.Door.DoorClient> GetDoorClientAsync(uint deviceID)
        {
            if (!_gatewayClient.IsConnected || _gatewayClient.CurrentChannel == null)
            {
                _logger.LogWarning("Gateway client not connected or no channel available");
                return null;
            }

            // Fast path: check if client already exists
            if (_doorClients.TryGetValue(deviceID, out var existingClient))
            {
                return existingClient;
            }

            // Slow path: create new client
            if (!await _clientSemaphore.WaitAsync(1000))
            {
                _logger.LogWarning("Timeout waiting for client semaphore");
                return null;
            }

            try
            {
                // Double-check pattern
                if (_doorClients.TryGetValue(deviceID, out existingClient))
                {
                    return existingClient;
                }

                // Create new door client using the gateway's current channel
                var doorClient = new Door.Door.DoorClient(_gatewayClient.CurrentChannel);
                _doorClients.TryAdd(deviceID, doorClient);
                return doorClient;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create door client for device {DeviceID}", deviceID);
                return null;
            }
            finally
            {
                _clientSemaphore.Release();
            }
        }

        private T GetCachedData<T>(string key, T defaultValue = default(T))
        {
            if (_cache.TryGetValue(key, out var cached) &&
                cached.Expiry > DateTime.UtcNow &&
                cached.Data is T data)
            {
                return data;
            }
            return defaultValue;
        }

        private void SetCachedData<T>(string key, T data)
        {
            var expiry = DateTime.UtcNow.AddSeconds(CACHE_DURATION_SECONDS);
            _cache.TryAdd(key, (expiry, data));
        }

        private async Task<T> ExecuteWithRetryAsync<T>(
            Func<Door.Door.DoorClient, Task<T>> operation,
            uint deviceID,
            T defaultValue = default(T),
            string cacheKey = null)
        {
            // Check cache first
            if (!string.IsNullOrEmpty(cacheKey))
            {
                var cached = GetCachedData<T>(cacheKey);
                if (cached != null && !cached.Equals(default(T)))
                {
                    return cached;
                }
            }

            try
            {
                var client = await GetDoorClientAsync(deviceID);
                if (client == null)
                {
                    _logger.LogWarning("No door client available for device {DeviceID}", deviceID);
                    return defaultValue;
                }

                var result = await operation(client);

                // Cache successful results
                if (!string.IsNullOrEmpty(cacheKey) && result != null)
                {
                    SetCachedData(cacheKey, result);
                }

                return result;
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, "RPC error for device {DeviceID}: {Status}", deviceID, ex.Status);

                // Remove client on connection errors
                if (ex.StatusCode == StatusCode.Unavailable || ex.StatusCode == StatusCode.DeadlineExceeded)
                {
                    _doorClients.TryRemove(deviceID, out _);
                }

                return defaultValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error for device {DeviceID}", deviceID);
                return defaultValue;
            }
        }

        // Public methods
        public async Task<RepeatedField<DoorInfo>> GetListAsync(uint deviceID)
        {
            _logger.LogInformation("Getting door list for device {DeviceID}", deviceID);

            var cacheKey = $"doorlist_{deviceID}";
            return await ExecuteWithRetryAsync(
                async client =>
                {
                    var request = new GetListRequest { DeviceID = deviceID };
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

                    var callOptions = new CallOptions(
                        deadline: DateTime.UtcNow.AddSeconds(5),
                        cancellationToken: cts.Token
                    );

                    var response = await client.GetListAsync(request, callOptions);
                    _logger.LogInformation("Retrieved {Count} doors for device {DeviceID}",
                        response?.Doors?.Count ?? 0, deviceID);

                    return response?.Doors ?? new RepeatedField<DoorInfo>();
                },
                deviceID,
                new RepeatedField<DoorInfo>(),
                cacheKey
            );
        }

        public async Task<List<DoorInfoModel>> GetAllDoorsAsync(uint deviceID)
        {
            _logger.LogInformation("Getting all door models for device {DeviceID}", deviceID);

            try
            {
                // Get door list and status in parallel for better performance
                var doorListTask = GetListAsync(deviceID);
                var statusTask = GetStatusAsync(deviceID);

                await Task.WhenAll(doorListTask, statusTask);

                var doors = await doorListTask;
                var statuses = await statusTask;

                var result = new List<DoorInfoModel>();

                if (doors?.Count > 0)
                {
                    foreach (var door in doors)
                    {
                        var status = statuses?.FirstOrDefault(s => s.DoorID == door.DoorID);

                        result.Add(new DoorInfoModel
                        {
                            DoorID = door.DoorID,
                            Name = door.Name ?? $"Door {door.DoorID}",
                            DeviceID = deviceID,
                            RelayPort = (byte)(door.Relay?.Port ?? 0),
                            Status = status?.IsUnlocked == true ? "Unlocked" : "Locked",
                            LastActivity = DateTime.Now
                        });
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all doors for device {DeviceID}", deviceID);
                return new List<DoorInfoModel>();
            }
        }

        public async Task<RepeatedField<Door.Status>> GetStatusAsync(uint deviceID)
        {
            _logger.LogInformation("Getting door status for device {DeviceID}", deviceID);

            var cacheKey = $"doorstatus_{deviceID}_{DateTime.UtcNow.Minute}"; // Cache for 1 minute
            return await ExecuteWithRetryAsync(
                async client =>
                {
                    var request = new GetStatusRequest { DeviceID = deviceID };
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

                    var callOptions = new CallOptions(
                        deadline: DateTime.UtcNow.AddSeconds(3),
                        cancellationToken: cts.Token
                    );

                    var response = await client.GetStatusAsync(request, callOptions);
                    _logger.LogInformation("Retrieved {Count} door statuses for device {DeviceID}",
                        response?.Status?.Count ?? 0, deviceID);

                    return response?.Status ?? new RepeatedField<Door.Status>();
                },
                deviceID,
                new RepeatedField<Door.Status>(),
                cacheKey
            );
        }

        public async Task<bool> AddAsync(uint deviceID, IEnumerable<DoorInfo> doors)
        {
            if (doors == null || !doors.Any())
            {
                _logger.LogWarning("Attempted to add doors with empty collection for device {DeviceID}", deviceID);
                return false;
            }

            _logger.LogInformation("Adding {Count} doors for device {DeviceID}", doors.Count(), deviceID);

            return await ExecuteWithRetryAsync(
                async client =>
                {
                    var request = new AddRequest { DeviceID = deviceID };
                    request.Doors.AddRange(doors);

                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    var callOptions = new CallOptions(
                        deadline: DateTime.UtcNow.AddSeconds(10),
                        cancellationToken: cts.Token
                    );

                    await client.AddAsync(request, callOptions);
                    _logger.LogInformation("Successfully added doors for device {DeviceID}", deviceID);

                    // Invalidate cache
                    var cacheKey = $"doorlist_{deviceID}";
                    _cache.TryRemove(cacheKey, out _);

                    return true;
                },
                deviceID,
                false
            );
        }

        public async Task<bool> DeleteAsync(uint deviceID, IEnumerable<uint> doorIDs)
        {
            if (doorIDs == null || !doorIDs.Any())
            {
                _logger.LogWarning("Attempted to delete doors with empty collection for device {DeviceID}", deviceID);
                return false;
            }

            _logger.LogInformation("Deleting {Count} doors for device {DeviceID}", doorIDs.Count(), deviceID);

            return await ExecuteWithRetryAsync(
                async client =>
                {
                    var request = new DeleteRequest { DeviceID = deviceID };
                    request.DoorIDs.AddRange(doorIDs);

                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                    var callOptions = new CallOptions(
                        deadline: DateTime.UtcNow.AddSeconds(8),
                        cancellationToken: cts.Token
                    );

                    await client.DeleteAsync(request, callOptions);
                    _logger.LogInformation("Successfully deleted doors for device {DeviceID}", deviceID);

                    // Invalidate cache
                    var cacheKey = $"doorlist_{deviceID}";
                    _cache.TryRemove(cacheKey, out _);

                    return true;
                },
                deviceID,
                false
            );
        }

        public async Task<bool> DeleteAllAsync(uint deviceID)
        {
            _logger.LogInformation("Deleting all doors for device {DeviceID}", deviceID);

            return await ExecuteWithRetryAsync(
                async client =>
                {
                    var request = new DeleteAllRequest { DeviceID = deviceID };

                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    var callOptions = new CallOptions(
                        deadline: DateTime.UtcNow.AddSeconds(10),
                        cancellationToken: cts.Token
                    );

                    await client.DeleteAllAsync(request, callOptions);
                    _logger.LogInformation("Successfully deleted all doors for device {DeviceID}", deviceID);

                    // Invalidate cache
                    var cacheKey = $"doorlist_{deviceID}";
                    _cache.TryRemove(cacheKey, out _);

                    return true;
                },
                deviceID,
                false
            );
        }

        public async Task<bool> LockAsync(uint deviceID, IEnumerable<uint> doorIDs, Door.DoorFlag doorFlag = Door.DoorFlag.Operator)
        {
            if (doorIDs == null || !doorIDs.Any())
            {
                _logger.LogWarning("Attempted to lock doors with empty collection for device {DeviceID}", deviceID);
                return false;
            }

            _logger.LogInformation("Locking {Count} doors for device {DeviceID}", doorIDs.Count(), deviceID);

            return await ExecuteWithRetryAsync(
                async client =>
                {
                    var request = new LockRequest
                    {
                        DeviceID = deviceID,
                        DoorFlag = (uint)doorFlag
                    };
                    request.DoorIDs.AddRange(doorIDs);

                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(6));
                    var callOptions = new CallOptions(
                        deadline: DateTime.UtcNow.AddSeconds(6),
                        cancellationToken: cts.Token
                    );

                    await client.LockAsync(request, callOptions);
                    _logger.LogInformation("Successfully locked doors for device {DeviceID}", deviceID);

                    // Invalidate status cache
                    InvalidateStatusCache(deviceID);

                    return true;
                },
                deviceID,
                false
            );
        }

        public async Task<bool> UnlockAsync(uint deviceID, IEnumerable<uint> doorIDs, Door.DoorFlag doorFlag = Door.DoorFlag.Operator)
        {
            if (doorIDs == null || !doorIDs.Any())
            {
                _logger.LogWarning("Attempted to unlock doors with empty collection for device {DeviceID}", deviceID);
                return false;
            }

            _logger.LogInformation("Unlocking {Count} doors for device {DeviceID}", doorIDs.Count(), deviceID);

            return await ExecuteWithRetryAsync(
                async client =>
                {
                    var request = new UnlockRequest
                    {
                        DeviceID = deviceID,
                        DoorFlag = (uint)doorFlag
                    };
                    request.DoorIDs.AddRange(doorIDs);

                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(6));
                    var callOptions = new CallOptions(
                        deadline: DateTime.UtcNow.AddSeconds(6),
                        cancellationToken: cts.Token
                    );

                    await client.UnlockAsync(request, callOptions);
                    _logger.LogInformation("Successfully unlocked doors for device {DeviceID}", deviceID);

                    // Invalidate status cache
                    InvalidateStatusCache(deviceID);

                    return true;
                },
                deviceID,
                false
            );
        }

        public async Task<bool> ReleaseAsync(uint deviceID, IEnumerable<uint> doorIDs, Door.DoorFlag doorFlag = Door.DoorFlag.Operator)
        {
            if (doorIDs == null || !doorIDs.Any())
            {
                _logger.LogWarning("Attempted to release doors with empty collection for device {DeviceID}", deviceID);
                return false;
            }

            _logger.LogInformation("Releasing {Count} doors for device {DeviceID}", doorIDs.Count(), deviceID);

            return await ExecuteWithRetryAsync(
                async client =>
                {
                    var request = new ReleaseRequest
                    {
                        DeviceID = deviceID,
                        DoorFlag = (uint)doorFlag
                    };
                    request.DoorIDs.AddRange(doorIDs);

                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(6));
                    var callOptions = new CallOptions(
                        deadline: DateTime.UtcNow.AddSeconds(6),
                        cancellationToken: cts.Token
                    );

                    await client.ReleaseAsync(request, callOptions);
                    _logger.LogInformation("Successfully released doors for device {DeviceID}", deviceID);

                    // Invalidate status cache
                    InvalidateStatusCache(deviceID);

                    return true;
                },
                deviceID,
                false
            );
        }

        public async Task<bool> SetAlarmAsync(uint deviceID, IEnumerable<uint> doorIDs, Door.AlarmFlag alarmFlag)
        {
            if (doorIDs == null || !doorIDs.Any())
            {
                _logger.LogWarning("Attempted to set alarm on doors with empty collection for device {DeviceID}", deviceID);
                return false;
            }

            _logger.LogInformation("Setting alarm on {Count} doors for device {DeviceID}", doorIDs.Count(), deviceID);

            return await ExecuteWithRetryAsync(
                async client =>
                {
                    var request = new SetAlarmRequest
                    {
                        DeviceID = deviceID,
                        AlarmFlag = (uint)alarmFlag
                    };
                    request.DoorIDs.AddRange(doorIDs);

                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(6));
                    var callOptions = new CallOptions(
                        deadline: DateTime.UtcNow.AddSeconds(6),
                        cancellationToken: cts.Token
                    );

                    await client.SetAlarmAsync(request, callOptions);
                    _logger.LogInformation("Successfully set alarm for device {DeviceID}", deviceID);

                    return true;
                },
                deviceID,
                false
            );
        }

        private void InvalidateStatusCache(uint deviceID)
        {
            var keysToRemove = _cache.Keys
                .Where(k => k.StartsWith($"doorstatus_{deviceID}_"))
                .ToList();

            foreach (var key in keysToRemove)
            {
                _cache.TryRemove(key, out _);
            }
        }

        public static DoorInfo CreateDoorInfo(string name, uint doorID, uint deviceID, uint relayPort)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Door name cannot be empty", nameof(name));

            if (doorID == 0)
                throw new ArgumentException("Door ID cannot be 0", nameof(doorID));

            if (deviceID == 0)
                throw new ArgumentException("Device ID cannot be 0", nameof(deviceID));

            if (relayPort > 255)
                throw new ArgumentException("Relay port must be between 0 and 255", nameof(relayPort));

            return new DoorInfo
            {
                DoorID = doorID,
                Name = name,
                EntryDeviceID = deviceID,
                ExitDeviceID = deviceID,
                Relay = new Relay { DeviceID = deviceID, Port = relayPort },
                Sensor = new Sensor { DeviceID = deviceID, Port = 0 },
                Button = new ExitButton { DeviceID = deviceID, Port = 0 },
                AutoLockTimeout = 5,
                HeldOpenTimeout = 30
            };
        }

        public void Dispose()
        {
            _cacheCleanupTimer?.Dispose();
            _clientSemaphore?.Dispose();
            _doorClients.Clear();
            _cache.Clear();
        }
    }
}