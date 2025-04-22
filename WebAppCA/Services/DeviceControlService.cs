using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WebAppCA.SDK;

namespace WebAppCA.Services
{
    public class DeviceControlService
    {
        private readonly ILogger<DeviceControlService> _logger;
        private readonly SupremaSDKService _sdkService;

        public DeviceControlService(ILogger<DeviceControlService> logger, SupremaSDKService sdkService)
        {
            _logger = logger;
            _sdkService = sdkService;
        }

        // Obtenir l'heure de l'appareil
        public async Task<DeviceTimeResult> GetDeviceTimeAsync(uint deviceId)
        {
            _logger.LogInformation("Getting device time for device ID: {DeviceId}", deviceId);

            // Vérifier que l'appareil est connecté
            if (!await EnsureDeviceConnectedAsync(deviceId))
            {
                return new DeviceTimeResult { Success = false, ErrorMessage = "Device not connected" };
            }

            // Obtenir l'heure de l'appareil
            uint timestamp = 0;
            BS2ErrorCode result = (BS2ErrorCode)API.BS2_GetDeviceTime(_sdkService.GetContext(), deviceId, out timestamp);

            if (result != BS2ErrorCode.BS_SDK_SUCCESS)
            {
                _logger.LogError("Failed to get device time. Error: {Error}", result);
                return new DeviceTimeResult { Success = false, ErrorMessage = $"Error: {result}" };
            }

            DateTime deviceTime = Util.ConvertFromUnixTimestamp(timestamp);
            _logger.LogInformation("Device time: {DeviceTime}", deviceTime);

            return new DeviceTimeResult { Success = true, Time = deviceTime };
        }

        // Définir l'heure de l'appareil
        public async Task<BaseResult> SetDeviceTimeAsync(uint deviceId, DateTime? time = null)
        {
            _logger.LogInformation("Setting device time for device ID: {DeviceId}", deviceId);

            // Vérifier que l'appareil est connecté
            if (!await EnsureDeviceConnectedAsync(deviceId))
            {
                return new BaseResult { Success = false, ErrorMessage = "Device not connected" };
            }

            // Définir l'heure de l'appareil
            uint timestamp = Convert.ToUInt32(Util.ConvertToUnixTimestamp(time ?? DateTime.Now));
            BS2ErrorCode result = (BS2ErrorCode)API.BS2_SetDeviceTime(_sdkService.GetContext(), deviceId, timestamp);

            if (result != BS2ErrorCode.BS_SDK_SUCCESS)
            {
                _logger.LogError("Failed to set device time. Error: {Error}", result);
                return new BaseResult { Success = false, ErrorMessage = $"Error: {result}" };
            }

            _logger.LogInformation("Device time set successfully");
            return new BaseResult { Success = true };
        }

        // Redémarrer l'appareil
        public async Task<BaseResult> RebootDeviceAsync(uint deviceId)
        {
            _logger.LogInformation("Rebooting device ID: {DeviceId}", deviceId);

            // Vérifier que l'appareil est connecté
            if (!await EnsureDeviceConnectedAsync(deviceId))
            {
                return new BaseResult { Success = false, ErrorMessage = "Device not connected" };
            }

            // Redémarrer l'appareil
            BS2ErrorCode result = (BS2ErrorCode)API.BS2_RebootDevice(_sdkService.GetContext(), deviceId);

            if (result != BS2ErrorCode.BS_SDK_SUCCESS)
            {
                _logger.LogError("Failed to reboot device. Error: {Error}", result);
                return new BaseResult { Success = false, ErrorMessage = $"Error: {result}" };
            }

            _logger.LogInformation("Device reboot command sent successfully");
            return new BaseResult { Success = true };
        }

        // Verrouiller l'appareil
        public async Task<BaseResult> LockDeviceAsync(uint deviceId)
        {
            _logger.LogInformation("Locking device ID: {DeviceId}", deviceId);

            // Vérifier que l'appareil est connecté
            if (!await EnsureDeviceConnectedAsync(deviceId))
            {
                return new BaseResult { Success = false, ErrorMessage = "Device not connected" };
            }

            // Verrouiller l'appareil
            BS2ErrorCode result = (BS2ErrorCode)API.BS2_LockDevice(_sdkService.GetContext(), deviceId);

            if (result != BS2ErrorCode.BS_SDK_SUCCESS)
            {
                _logger.LogError("Failed to lock device. Error: {Error}", result);
                return new BaseResult { Success = false, ErrorMessage = $"Error: {result}" };
            }

            _logger.LogInformation("Device locked successfully");
            return new BaseResult { Success = true };
        }

        // Déverrouiller l'appareil
        public async Task<BaseResult> UnlockDeviceAsync(uint deviceId)
        {
            _logger.LogInformation("Unlocking device ID: {DeviceId}", deviceId);

            // Vérifier que l'appareil est connecté
            if (!await EnsureDeviceConnectedAsync(deviceId))
            {
                return new BaseResult { Success = false, ErrorMessage = "Device not connected" };
            }

            // Déverrouiller l'appareil
            BS2ErrorCode result = (BS2ErrorCode)API.BS2_UnlockDevice(_sdkService.GetContext(), deviceId);

            if (result != BS2ErrorCode.BS_SDK_SUCCESS)
            {
                _logger.LogError("Failed to unlock device. Error: {Error}", result);
                return new BaseResult { Success = false, ErrorMessage = $"Error: {result}" };
            }

            _logger.LogInformation("Device unlocked successfully");
            return new BaseResult { Success = true };
        }

        // Réinitialiser l'appareil aux paramètres d'usine
        public async Task<BaseResult> FactoryResetAsync(uint deviceId)
        {
            _logger.LogInformation("Factory resetting device ID: {DeviceId}", deviceId);

            // Vérifier que l'appareil est connecté
            if (!await EnsureDeviceConnectedAsync(deviceId))
            {
                return new BaseResult { Success = false, ErrorMessage = "Device not connected" };
            }

            // Réinitialiser l'appareil
            BS2ErrorCode result = (BS2ErrorCode)API.BS2_FactoryReset(_sdkService.GetContext(), deviceId);

            if (result != BS2ErrorCode.BS_SDK_SUCCESS)
            {
                _logger.LogError("Failed to factory reset device. Error: {Error}", result);
                return new BaseResult { Success = false, ErrorMessage = $"Error: {result}" };
            }

            _logger.LogInformation("Device factory reset successfully");
            return new BaseResult { Success = true };
        }

        // Obtenir les informations sur l'appareil
        public async Task<DeviceInfoResult> GetDeviceInfoAsync(uint deviceId)
        {
            _logger.LogInformation("Getting device info for device ID: {DeviceId}", deviceId);

            // Vérifier que l'appareil est connecté
            if (!await EnsureDeviceConnectedAsync(deviceId))
            {
                return new DeviceInfoResult { Success = false, ErrorMessage = "Device not connected" };
            }

            // Obtenir les informations sur l'appareil
            BS2SimpleDeviceInfo deviceInfo;
            BS2SimpleDeviceInfoEx deviceInfoEx;

            BS2ErrorCode result = (BS2ErrorCode)API.BS2_GetDeviceInfoEx(_sdkService.GetContext(), deviceId, out deviceInfo, out deviceInfoEx);

            if (result != BS2ErrorCode.BS_SDK_SUCCESS)
            {
                _logger.LogError("Failed to get device info. Error: {Error}", result);
                return new DeviceInfoResult { Success = false, ErrorMessage = $"Error: {result}" };
            }

            return new DeviceInfoResult
            {
                Success = true,
                DeviceId = deviceInfo.deviceID,
                IpAddress = deviceInfo.ipAddress,
                Port = deviceInfo.port,
                ModelName = deviceInfo.modelName,
                FirmwareVersion = deviceInfoEx.firmwareVersion
            };
        }

        // Méthode utilitaire pour s'assurer qu'un appareil est connecté
        private async Task<bool> EnsureDeviceConnectedAsync(uint deviceId)
        {
            return await _sdkService.ConnectDeviceAsync(deviceId);
        }
    }

    // Classes de résultats
    public class BaseResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class DeviceTimeResult : BaseResult
    {
        public DateTime Time { get; set; }
    }

    public class DeviceInfoResult : BaseResult
    {
        public uint DeviceId { get; set; }
        public string IpAddress { get; set; }
        public ushort Port { get; set; }
        public string ModelName { get; set; }
        public ushort FirmwareVersion { get; set; }
    }
}