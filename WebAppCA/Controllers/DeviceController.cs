using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using WebAppCA.Models;

namespace WebAppCA.Controllers
{
    public class DeviceController : Controller
    {
        private readonly ILogger<DeviceController> _logger;

        public DeviceController(ILogger<DeviceController> logger)
        {
            _logger = logger;
        }

        // === P/Invoke des fonctions SDK ===
        private const string DllName = "BS_SDK_V2.dll";

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int BS2_Initialize();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int BS2_Release();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int BS2_ConnectDevice(IntPtr context, uint deviceId, out IntPtr device);

        // Structure pour les informations de l'appareil (à adapter selon le SDK)
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct BS2DeviceInfo
        {
            public uint id;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string name;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
            public string ip;
            public ushort port;
            // Ajouter d'autres champs selon la documentation du SDK
        }

        // GET: /Device/Index
        public IActionResult Index()
        {
            var devices = new List<DeviceInfoModel>();
            return View("~/Views/Home/Index.cshtml", devices);
        }

        // POST: /Device/ScanDevice
        [HttpPost]
        public IActionResult ScanDevice(string ip, int port)
        {
            try
            {
                // Initialiser le SDK
                int result = BS2_Initialize();
                if (result != 0)
                {
                    _logger.LogError($"Échec de l'initialisation du SDK. Code d'erreur: {result}");
                    return StatusCode(500, $"Erreur d'initialisation du SDK. Code: {result}");
                }

                // Ici vous devriez normalement utiliser BS2_SearchDeviceViaIP ou similaire
                // Pour l'exemple, nous simulons un appareil trouvé
                var device = new DeviceInfoModel
                {
                    DeviceID = 1, // Changed to uint value
                    DeviceName = "Device Demo",
                    IPAddress = ip,
                    Port = port,
                    ConnectionStatus = "Connected"
                };

                // Libérer le SDK après utilisation
                BS2_Release();

                return Ok(new List<DeviceInfoModel> { device });
            }
            catch (DllNotFoundException ex)
            {
                _logger.LogError(ex, "DLL du SDK non trouvée");
                return StatusCode(500, "DLL du SDK BioStark non trouvée. Vérifiez l'installation.");
            }
            catch (AccessViolationException ex)
            {
                _logger.LogError(ex, "Erreur d'accès mémoire lors de l'utilisation du SDK");
                return StatusCode(500, "Erreur d'accès mémoire avec le SDK BioStark");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur inattendue lors de la numérisation des appareils");
                return StatusCode(500, "Erreur interne du serveur");
            }
        }

        // POST: /Device/Connect
        [HttpPost]
        public IActionResult Connect(uint deviceID)
        {
            try
            {
                BS2_Initialize();

                IntPtr devicePtr;
                int result = BS2_ConnectDevice(IntPtr.Zero, deviceID, out devicePtr);

                if (result == 0)
                {
                    TempData["Message"] = $"Connecté avec succès à l'appareil {deviceID}";
                }
                else
                {
                    TempData["Error"] = $"Échec de la connexion à l'appareil {deviceID}. Code d'erreur: {result}";
                }

                BS2_Release();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la connexion à l'appareil {deviceID}");
                TempData["Error"] = $"Erreur lors de la connexion à l'appareil {deviceID}";
            }

            return RedirectToAction("Index");
        }

        // POST: /Device/ReadLogs
        [HttpPost]
        public IActionResult ReadLogs(uint deviceID)
        {
            // Implémentation similaire à Connect, utilisant BS2_GetLog
            TempData["Message"] = $"Logs lus pour l'appareil {deviceID} (simulation)";
            return RedirectToAction("Index");
        }

        // POST: /Device/Reboot
        [HttpPost]
        public IActionResult Reboot(uint deviceID)
        {
            // Implémentation utilisant BS2_RebootDevice
            TempData["Message"] = $"Appareil {deviceID} redémarré (simulation)";
            return RedirectToAction("Index");
        }

        // POST: /Device/Reset
        [HttpPost]
        public IActionResult Reset(uint deviceID)
        {
            // Implémentation utilisant BS2_FactoryReset
            TempData["Message"] = $"Appareil {deviceID} réinitialisé (simulation)";
            return RedirectToAction("Index");
        }
    }
}