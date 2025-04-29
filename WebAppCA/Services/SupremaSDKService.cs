using System.Runtime.InteropServices;
using System.Text;
using WebAppCA.Models;
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

            Initialize();
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
            public DeviceControlService.DeviceStatus Status { get; set; }
        }

        public bool Initialize()
        {
            if (_isInitialized) return true;

            Console.WriteLine("Initializing Suprema SDK");

            int result = BS2_Initialize(out _sdkContext);
            if (result != 0)
            {
                Console.WriteLine($"Failed to initialize SDK. Code: {result}");
                return false;
            }

            _isInitialized = true;
            Console.WriteLine("SDK initialized");
            return true;
        }

        [DllImport("BS_SDK_V2.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int BS2_Initialize(out IntPtr context);

        [DllImport("BS_SDK_V2.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int BS2_ReleaseContext(IntPtr context);

        public IntPtr GetContext()
        {
            CheckInitialized();
            return _sdkContext;
        }

        public async Task<DeviceInfo> ConnectDeviceAsync(string ipAddress, ushort port = 51211)
        {
            if (!_isInitialized && !Initialize()) return null;

            _logger.LogInformation("Connecting to device at {IP}:{Port}", ipAddress, port);

            uint deviceId;
            int result = SupremaSDK.BS2_ConnectDevice(_sdkContext, ipAddress, port, out deviceId);
            if (result != (int)BS2ErrorCode.BS_SDK_SUCCESS) return null;

            var deviceInfo = await GetDeviceInfoAsync(deviceId);
            if (deviceInfo != null)
            {
                deviceInfo.IsConnected = true;
                _connectedDevices[deviceId] = deviceInfo;
            }

            return deviceInfo;
        }

        public async Task<bool> ConnectDeviceAsync(uint deviceId)
        {
            if (!_isInitialized && !Initialize()) return false;

            if (_connectedDevices.ContainsKey(deviceId) && _connectedDevices[deviceId].IsConnected) return true;

            var deviceInfo = await GetDeviceInfoAsync(deviceId);
            if (deviceInfo != null)
            {
                deviceInfo.IsConnected = true;
                _connectedDevices[deviceId] = deviceInfo;
                return true;
            }

            return false;
        }

        public async Task<bool> DisconnectDeviceAsync(uint deviceId)
        {
            if (!_isInitialized && !Initialize()) return false;

            int result = SupremaSDK.BS2_DisconnectDevice(_sdkContext, deviceId);
            if (result != (int)BS2ErrorCode.BS_SDK_SUCCESS) return false;

            _connectedDevices.Remove(deviceId);
            return true;
        }

        public async Task<DeviceInfo> GetDeviceInfoAsync(uint deviceId)
        {
            if (!_isInitialized && !Initialize()) return null;

            BS2SimpleDeviceInfo deviceInfo;
            BS2SimpleDeviceInfoEx deviceInfoEx;

            int result = SupremaSDK.BS2_GetDeviceInfoEx(_sdkContext, deviceId, out deviceInfo, out deviceInfoEx);
            if (result != (int)BS2ErrorCode.BS_SDK_SUCCESS) return null;

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
            if (!_isInitialized && !Initialize()) return null;

            uint timestamp;
            int result = SupremaSDK.BS2_GetDeviceTime(_sdkContext, deviceId, out timestamp);
            if (result != (int)BS2ErrorCode.BS_SDK_SUCCESS) return null;

            return DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime;
        }

        /*public async Task<bool> SetDeviceTimeAsync(uint deviceId, DateTime? dateTime = null)
        {
            if (!_isInitialized && !Initialize()) return false;

            uint timestamp = (uint)(dateTime ?? DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
            int result = SupremaSDK.BS2_SetDeviceTime(_sdkContext, deviceId, timestamp);
            return result == (int)BS2ErrorCode.BS_SDK_SUCCESS;
        }
        */
        public async Task<bool> RebootDeviceAsync(uint deviceId)
        {
            if (!_isInitialized && !Initialize()) return false;

            int result = SupremaSDK.BS2_RebootDevice(_sdkContext, deviceId);
            return result == (int)BS2ErrorCode.BS_SDK_SUCCESS;
        }

        public async Task<bool> FactoryResetAsync(uint deviceId)
        {
            if (!_isInitialized && !Initialize()) return false;

            int result = SupremaSDK.BS2_FactoryReset(_sdkContext, deviceId);
            return result == (int)BS2ErrorCode.BS_SDK_SUCCESS;
        }

        public async Task<bool> LockDeviceAsync(uint deviceId)
        {
            if (!_isInitialized && !Initialize()) return false;

            int result = SupremaSDK.BS2_LockDevice(_sdkContext, deviceId);
            return result == (int)BS2ErrorCode.BS_SDK_SUCCESS;
        }

        public async Task<bool> UnlockDeviceAsync(uint deviceId)
        {
            if (!_isInitialized && !Initialize()) return false;

            int result = SupremaSDK.BS2_UnlockDevice(_sdkContext, deviceId);
            return result == (int)BS2ErrorCode.BS_SDK_SUCCESS;
        }

        public IEnumerable<DeviceInfo> GetConnectedDevices()
        {
            return _connectedDevices.Values;
        }

        public List<DeviceInfoModel> GetConnectedDevicesAsModels()
        {
            var models = new List<DeviceInfoModel>();
            foreach (var device in _connectedDevices.Values)
            {
                models.Add(new DeviceInfoModel
                {
                    DeviceID = device.DeviceId,
                    DeviceName = device.DeviceName,
                    IPAddress = device.IpAddress,
                    Port = device.Port,
                    ConnectionStatus = device.IsConnected ? "Connecté" : "Déconnecté"
                });
            }
            return models;
        }

        private void CheckInitialized()
        {
            if (!_isInitialized && !Initialize())
            {
                throw new InvalidOperationException("SDK is not initialized");
            }
        }

        public void Dispose()
        {
            if (_isInitialized && _sdkContext != IntPtr.Zero)
            {
                foreach (var deviceId in _connectedDevices.Keys)
                {
                    SupremaSDK.BS2_DisconnectDevice(_sdkContext, deviceId);
                }

                BS2_ReleaseContext(_sdkContext);
                _sdkContext = IntPtr.Zero;
                _isInitialized = false;
                _connectedDevices.Clear();

                _logger.LogInformation("Suprema SDK released");
            }
        }
    }
}
