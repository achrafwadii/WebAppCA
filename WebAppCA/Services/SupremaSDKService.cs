using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WebAppCA.SDK;

namespace WebAppCA.Services
{
    public class SupremaSDKService : IDisposable
    {
        private readonly ILogger<SupremaSDKService> _logger;
        private IntPtr _sdkContext;
        private bool _isInitialized;
        private readonly Dictionary<uint, DeviceInfo> _connectedDevices;

        public SupremaSDKService(ILogger<SupremaSDKService> logger)
        {
            _logger = logger;
            _sdkContext = IntPtr.Zero;
            _isInitialized = false;
            _connectedDevices = new Dictionary<uint, DeviceInfo>();
        }

        public class DeviceInfo
        {
            public uint DeviceId { get; set; }
            public string IpAddress { get; set; }
            public string MacAddress { get; set; }
            public string DeviceName { get; set; }
            public ushort Type { get; set; }
            public ushort Port { get; set; }
            public bool IsConnected { get; set; }
        }

        public bool Initialize()
        {
            if (_isInitialized)
                return true;

            int result = SupremaSDK.BS2_Initialize(out _sdkContext);
            if (result != (int)BS2ErrorCode.BS_SDK_SUCCESS)
            {
                _logger.LogError("Failed to initialize Suprema SDK. Error: {Error}", result);
                return false;
            }

            _isInitialized = true;
            _logger.LogInformation("Suprema SDK initialized successfully");
            return true;
        }

        public IntPtr GetContext()
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("SDK is not initialized");
            }

            return _sdkContext;
        }

        public async Task<DeviceInfo> ConnectDeviceAsync(string ipAddress, ushort port = 51211)
        {
            CheckInitialized();

            _logger.LogInformation("Connecting to device at {IP}:{Port}", ipAddress, port);

            uint deviceId = 0;
            int result = SupremaSDK.BS2_ConnectDevice(_sdkContext, ipAddress, port, out deviceId);

            if (result != (int)BS2ErrorCode.BS_SDK_SUCCESS)
            {
                _logger.LogError("Failed to connect to device. Error: {Error}", result);
                return null;
            }

            var deviceInfo = await GetDeviceInfoAsync(deviceId);
            if (deviceInfo != null)
            {
                deviceInfo.IsConnected = true;
                _connectedDevices[deviceId] = deviceInfo;
            }

            return deviceInfo;
        }

        public async Task<bool> DisconnectDeviceAsync(uint deviceId)
        {
            CheckInitialized();

            _logger.LogInformation("Disconnecting device ID: {DeviceId}", deviceId);

            int result = SupremaSDK.BS2_DisconnectDevice(_sdkContext, deviceId);

            if (result != (int)BS2ErrorCode.BS_SDK_SUCCESS)
            {
                _logger.LogError("Failed to disconnect device. Error: {Error}", result);
                return false;
            }

            if (_connectedDevices.ContainsKey(deviceId))
            {
                _connectedDevices.Remove(deviceId);
            }

            return true;
        }

        public async Task<DeviceInfo> GetDeviceInfoAsync(uint deviceId)
        {
            CheckInitialized();

            BS2SimpleDeviceInfo deviceInfo;
            BS2SimpleDeviceInfoEx deviceInfoEx;

            int result = SupremaSDK.BS2_GetDeviceInfoEx(_sdkContext, deviceId, out deviceInfo, out deviceInfoEx);

            if (result != (int)BS2ErrorCode.BS_SDK_SUCCESS)
            {
                _logger.LogError("Failed to get device info. Error: {Error}", result);
                return null;
            }

            return new DeviceInfo
            {
                DeviceId = deviceInfo.deviceId,
                IpAddress = Encoding.ASCII.GetString(deviceInfo.ipAddr).TrimEnd('\0'),
                MacAddress = BitConverter.ToString(deviceInfo.macAddr).Replace("-", ":"),
                DeviceName = Encoding.ASCII.GetString(deviceInfoEx.deviceName).TrimEnd('\0'),
                Type = deviceInfo.type,
                Port = deviceInfo.port,
                IsConnected = true
            };
        }

        public async Task<DateTime?> GetDeviceTimeAsync(uint deviceId)
        {
            CheckInitialized();

            uint timestamp = 0;
            int result = SupremaSDK.BS2_GetDeviceTime(_sdkContext, deviceId, out timestamp);

            if (result != (int)BS2ErrorCode.BS_SDK_SUCCESS)
            {
                _logger.LogError("Failed to get device time. Error: {Error}", result);
                return null;
            }

            // Convert UNIX timestamp to DateTime
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddSeconds(timestamp);
        }

        public async Task<bool> SetDeviceTimeAsync(uint deviceId, DateTime? dateTime = null)
        {
            CheckInitialized();

            DateTime timeToSet = dateTime ?? DateTime.UtcNow;
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan elapsedTime = timeToSet.ToUniversalTime() - epoch;
            uint timestamp = (uint)elapsedTime.TotalSeconds;

            int result = SupremaSDK.BS2_SetDeviceTime(_sdkContext, deviceId, timestamp);

            if (result != (int)BS2ErrorCode.BS_SDK_SUCCESS)
            {
                _logger.LogError("Failed to set device time. Error: {Error}", result);
                return false;
            }

            return true;
        }

        public async Task<bool> RebootDeviceAsync(uint deviceId)
        {
            CheckInitialized();

            int result = SupremaSDK.BS2_RebootDevice(_sdkContext, deviceId);

            if (result != (int)BS2ErrorCode.BS_SDK_SUCCESS)
            {
                _logger.LogError("Failed to reboot device. Error: {Error}", result);
                return false;
            }

            return true;
        }

        public async Task<bool> FactoryResetAsync(uint deviceId)
        {
            CheckInitialized();

            int result = SupremaSDK.BS2_FactoryReset(_sdkContext, deviceId);

            if (result != (int)BS2ErrorCode.BS_SDK_SUCCESS)
            {
                _logger.LogError("Failed to factory reset device. Error: {Error}", result);
                return false;
            }

            return true;
        }

        public async Task<bool> LockDeviceAsync(uint deviceId)
        {
            CheckInitialized();

            int result = SupremaSDK.BS2_LockDevice(_sdkContext, deviceId);

            if (result != (int)BS2ErrorCode.BS_SDK_SUCCESS)
            {
                _logger.LogError("Failed to lock device. Error: {Error}", result);
                return false;
            }

            return true;
        }

        public async Task<bool> UnlockDeviceAsync(uint deviceId)
        {
            CheckInitialized();

            int result = SupremaSDK.BS2_UnlockDevice(_sdkContext, deviceId);

            if (result != (int)BS2ErrorCode.BS_SDK_SUCCESS)
            {
                _logger.LogError("Failed to unlock device. Error: {Error}", result);
                return false;
            }

            return true;
        }

        public IEnumerable<DeviceInfo> GetConnectedDevices()
        {
            return _connectedDevices.Values;
        }

        private void CheckInitialized()
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("SDK is not initialized");
            }
        }

        public void Dispose()
        {
            if (_isInitialized && _sdkContext != IntPtr.Zero)
            {
                // Disconnect all devices
                foreach (var deviceId in _connectedDevices.Keys)
                {
                    SupremaSDK.BS2_DisconnectDevice(_sdkContext, deviceId);
                }

                SupremaSDK.BS2_ReleaseContext(_sdkContext);
                _sdkContext = IntPtr.Zero;
                _isInitialized = false;
                _connectedDevices.Clear();
                _logger.LogInformation("Suprema SDK released");
            }
        }

        internal async Task<bool> ConnectDeviceAsync(uint deviceId)
        {
            throw new NotImplementedException();
        }
    }
}