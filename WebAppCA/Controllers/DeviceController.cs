using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebAppCA.Models;
using WebAppCA.Services;

namespace WebAppCA.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DeviceController : ControllerBase
    {
        private readonly ILogger<DeviceController> _logger;
        private readonly SupremaSDKService _sdkService;

        public DeviceController(
            ILogger<DeviceController> logger,
            SupremaSDKService sdkService)
        {
            _logger = logger;
            _sdkService = sdkService;
        }

        [HttpGet]
        public IActionResult GetConnectedDevices()
        {
            try
            {
                var devices = _sdkService.GetConnectedDevices();
                return Ok(devices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting connected devices");
                return StatusCode(500, new { Error = "Failed to get connected devices" });
            }
        }

        [HttpPost("connect")]
        public async Task<IActionResult> ConnectDevice([FromBody] ConnectDeviceRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.IpAddress))
                {
                    return BadRequest(new { Error = "IP address is required" });
                }

                var device = await _sdkService.ConnectDeviceAsync(request.IpAddress);
                if (device == null)
                {
                    return BadRequest(new { Error = "Failed to connect to device" });
                }

                return Ok(device);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting to device {IP}", request.IpAddress);
                return StatusCode(500, new { Error = "Failed to connect to device" });
            }
        }

        [HttpPost("{deviceId}/disconnect")]
        public async Task<IActionResult> DisconnectDevice(uint deviceId)
        {
            try
            {
                var result = await _sdkService.DisconnectDeviceAsync(deviceId);
                if (!result)
                {
                    return BadRequest(new { Error = "Failed to disconnect device" });
                }

                return Ok(new { Success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting device {DeviceId}", deviceId);
                return StatusCode(500, new { Error = "Failed to disconnect device" });
            }
        }

        [HttpGet("{deviceId}/time")]
        public async Task<IActionResult> GetDeviceTime(uint deviceId)
        {
            try
            {
                var time = await _sdkService.GetDeviceTimeAsync(deviceId);
                if (time == null)
                {
                    return BadRequest(new { Error = "Failed to get device time" });
                }

                return Ok(new { Time = time });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting device time for device {DeviceId}", deviceId);
                return StatusCode(500, new { Error = "Failed to get device time" });
            }
        }

        [HttpPost("{deviceId}/time")]
        public async Task<IActionResult> SetDeviceTime(uint deviceId, [FromBody] SetTimeRequest request)
        {
            try
            {
                var result = await _sdkService.SetDeviceTimeAsync(deviceId, request?.DateTime);
                if (!result)
                {
                    return BadRequest(new { Error = "Failed to set device time" });
                }

                return Ok(new { Success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting device time for device {DeviceId}", deviceId);
                return StatusCode(500, new { Error = "Failed to set device time" });
            }
        }

        [HttpPost("{deviceId}/reboot")]
        public async Task<IActionResult> RebootDevice(uint deviceId)
        {
            try
            {
                var result = await _sdkService.RebootDeviceAsync(deviceId);
                if (!result)
                {
                    return BadRequest(new { Error = "Failed to reboot device" });
                }

                return Ok(new { Success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rebooting device {DeviceId}", deviceId);
                return StatusCode(500, new { Error = "Failed to reboot device" });
            }
        }

        [HttpPost("{deviceId}/factoryReset")]
        public async Task<IActionResult> FactoryReset(uint deviceId)
        {
            try
            {
                var result = await _sdkService.FactoryResetAsync(deviceId);
                if (!result)
                {
                    return BadRequest(new { Error = "Failed to factory reset device" });
                }

                return Ok(new { Success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error factory resetting device {DeviceId}", deviceId);
                return StatusCode(500, new { Error = "Failed to factory reset device" });
            }
        }

        [HttpPost("{deviceId}/lock")]
        public async Task<IActionResult> LockDevice(uint deviceId)
        {
            try
            {
                var result = await _sdkService.LockDeviceAsync(deviceId);
                if (!result)
                {
                    return BadRequest(new { Error = "Failed to lock device" });
                }

                return Ok(new { Success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error locking device {DeviceId}", deviceId);
                return StatusCode(500, new { Error = "Failed to lock device" });
            }
        }

        [HttpPost("{deviceId}/unlock")]
        public async Task<IActionResult> UnlockDevice(uint deviceId)
        {
            try
            {
                var result = await _sdkService.UnlockDeviceAsync(deviceId);
                if (!result)
                {
                    return BadRequest(new { Error = "Failed to unlock device" });
                }

                return Ok(new { Success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unlocking device {DeviceId}", deviceId);
                return StatusCode(500, new { Error = "Failed to unlock device" });
            }
        }
    }
}