using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebAppCA.Models;
using WebAppCA.Services;

namespace WebAppCA.Controllers
{
    public class DeviceController : Controller
    {
        private readonly ILogger<DeviceController> _logger;
        private readonly SupremaSDKService _sdkService;
        private readonly DeviceControlService _deviceControlService;

        public DeviceController(
            ILogger<DeviceController> logger,
            SupremaSDKService sdkService,
            DeviceControlService deviceControlService)
        {
            _logger = logger;
            _sdkService = sdkService;
            _deviceControlService = deviceControlService;
        }

        // GET: /Device
        public IActionResult Index()
        {
            var devices = _sdkService.GetConnectedDevicesAsModels();
            return View(devices);
        }
        [HttpPost("scan")]
        public async Task<IActionResult> ScanDevice(string ip, int port = 51211)
        {
            try
            {
                if (string.IsNullOrEmpty(ip))
                {
                    return BadRequest(new { Error = "IP address is required" });
                }

                var device = await _sdkService.ConnectDeviceAsync(ip, (ushort)port);
                if (device == null)
                {
                    return BadRequest(new { Error = "Failed to connect to device" });
                }

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scanning device at {IP}:{Port}", ip, port);
                return StatusCode(500, new { Error = "Failed to scan device" });
            }
        }
        // POST: /Device/ScanDevice
        [HttpPost]
        public async Task<IActionResult> ScanDevice(string ip, ushort port = 51211)
        {
            _logger.LogInformation("Scanning device at IP: {IP}, Port: {Port}", ip, port);

            try
            {
                // Vérifier que les paramètres sont valides
                if (string.IsNullOrEmpty(ip))
                {
                    TempData["Error"] = "L'adresse IP est requise";
                    return RedirectToAction("Index");
                }

                // S'assurer que le SDK est initialisé
                if (!_sdkService.Initialize())
                {
                    TempData["Error"] = "Impossible d'initialiser le SDK Suprema";
                    return RedirectToAction("Index");
                }

                var device = await _sdkService.ConnectDeviceAsync(ip, port);
                if (device == null)
                {
                    TempData["Error"] = "Impossible de se connecter à l'appareil";
                }
                else
                {
                    TempData["Success"] = $"L'appareil {device.DeviceName} a été trouvé et connecté avec succès";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la connexion à l'appareil");
                TempData["Error"] = $"Une erreur s'est produite: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        // POST: /Device/Connect
        [HttpPost]
        public async Task<IActionResult> Connect(uint deviceId)
        {
            try
            {
                var result = await _sdkService.ConnectDeviceAsync(deviceId);
                if (!result)
                {
                    TempData["Error"] = "Impossible de se connecter à l'appareil";
                }
                else
                {
                    TempData["Success"] = "Appareil connecté avec succès";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la connexion à l'appareil {DeviceId}", deviceId);
                TempData["Error"] = $"Une erreur s'est produite: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        // POST: /Device/Disconnect
        [HttpPost]
        public async Task<IActionResult> Disconnect(uint deviceId)
        {
            try
            {
                var result = await _sdkService.DisconnectDeviceAsync(deviceId);
                if (!result)
                {
                    TempData["Error"] = "Échec de la déconnexion de l'appareil";
                }
                else
                {
                    TempData["Success"] = "Appareil déconnecté avec succès";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la déconnexion de l'appareil {DeviceId}", deviceId);
                TempData["Error"] = $"Une erreur s'est produite: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        // POST: /Device/ReadLogs
        [HttpPost]
        public async Task<IActionResult> ReadLogs(uint deviceId)
        {
            // Implémentez la logique pour lire les logs
            // Pour le moment, redirigez simplement
            TempData["Info"] = "Fonctionnalité de lecture des logs non implémentée";
            return RedirectToAction("Index");
        }

        // POST: /Device/Reboot
        [HttpPost]
        public async Task<IActionResult> Reboot(uint deviceId)
        {
            try
            {
                var result = await _sdkService.RebootDeviceAsync(deviceId);
                if (!result)
                {
                    TempData["Error"] = "Échec du redémarrage de l'appareil";
                }
                else
                {
                    TempData["Success"] = "Commande de redémarrage envoyée avec succès";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du redémarrage de l'appareil {DeviceId}", deviceId);
                TempData["Error"] = $"Une erreur s'est produite: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        // POST: /Device/Reset
        [HttpPost]
        public async Task<IActionResult> Reset(uint deviceId)
        {
            try
            {
                var result = await _sdkService.FactoryResetAsync(deviceId);
                if (!result)
                {
                    TempData["Error"] = "Échec de la réinitialisation de l'appareil";
                }
                else
                {
                    TempData["Success"] = "Appareil réinitialisé avec succès";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la réinitialisation de l'appareil {DeviceId}", deviceId);
                TempData["Error"] = $"Une erreur s'est produite: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        // POST: /Device/Lock
        [HttpPost]
        public async Task<IActionResult> Lock(uint deviceId)
        {
            try
            {
                var result = await _sdkService.LockDeviceAsync(deviceId);
                if (!result)
                {
                    TempData["Error"] = "Échec du verrouillage de l'appareil";
                }
                else
                {
                    TempData["Success"] = "Appareil verrouillé avec succès";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du verrouillage de l'appareil {DeviceId}", deviceId);
                TempData["Error"] = $"Une erreur s'est produite: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        // POST: /Device/Unlock
        [HttpPost]
        public async Task<IActionResult> Unlock(uint deviceId)
        {
            try
            {
                var result = await _sdkService.UnlockDeviceAsync(deviceId);
                if (!result)
                {
                    TempData["Error"] = "Échec du déverrouillage de l'appareil";
                }
                else
                {
                    TempData["Success"] = "Appareil déverrouillé avec succès";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du déverrouillage de l'appareil {DeviceId}", deviceId);
                TempData["Error"] = $"Une erreur s'est produite: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        // GET: /Device/GetTime
        [HttpGet]
        public async Task<IActionResult> GetTime(uint deviceId)
        {
            try
            {
                var time = await _sdkService.GetDeviceTimeAsync(deviceId);
                if (time == null)
                {
                    TempData["Error"] = "Impossible d'obtenir l'heure de l'appareil";
                }
                else
                {
                    TempData["Success"] = $"Heure de l'appareil: {time.Value.ToLocalTime()}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de l'heure de l'appareil {DeviceId}", deviceId);
                TempData["Error"] = $"Une erreur s'est produite: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        // POST: /Device/SetTime
        /*[HttpPost]
        public async Task<IActionResult> SetTime(uint deviceId, DateTime? dateTime = null)
        {
            try
            {
                var result = await _sdkService.SetDeviceTimeAsync(deviceId, dateTime);
                if (!result)
                {
                    TempData["Error"] = "Impossible de définir l'heure de l'appareil";
                }
                else
                {
                    TempData["Success"] = "Heure de l'appareil définie avec succès";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la définition de l'heure de l'appareil {DeviceId}", deviceId);
                TempData["Error"] = $"Une erreur s'est produite: {ex.Message}";
            }

            return RedirectToAction("Index");
        }*/
    }
}