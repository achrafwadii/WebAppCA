using Microsoft.AspNetCore.Mvc;
using WebAppCA.Models;
using WebAppCA.Services;
using Connect;
using System;
using System.Threading.Tasks;
using ConnectInfo = Connect.ConnectInfo;

namespace WebAppCA.Controllers
{
    public class DeviceController : Controller
    {
        private readonly ConnectSvc _connectSvc;
        private readonly DeviceDbService _deviceDbService; // Service pour accéder à la base de données

        public DeviceController(ConnectSvc connectSvc, DeviceDbService deviceDbService)
        {
            _connectSvc = connectSvc;
            _deviceDbService = deviceDbService;
        }

        [HttpPost]
        public IActionResult ConnectByIPAndPort(string ip, int port)
        {
            if (string.IsNullOrEmpty(ip) || port <= 0)
                return BadRequest("IP ou port invalide");
            try
            {
                var connectInfo = new ConnectInfo
                {
                    IPAddr = ip,
                    Port = port,
                    UseSSL = false  // Set according to your requirements
                };
                var deviceID = _connectSvc.Connect(connectInfo);
                TempData["Message"] = $"Équipement connecté : {ip}:{port} (ID: {deviceID})";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Erreur de connexion : {ex.Message}";
            }
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public IActionResult ConnectByDeviceID(int deviceID)
        {
            if (deviceID <= 0)
                return BadRequest("ID d'appareil invalide");

            try
            {
                // Récupérer les informations de l'appareil depuis la base de données
                var deviceInfo = _deviceDbService.GetDeviceById(deviceID);

                if (deviceInfo == null)
                {
                    TempData["Error"] = $"Appareil avec ID {deviceID} non trouvé";
                    return RedirectToAction("Index", "Home");
                }

                // Créer les informations de connexion à partir des données de l'appareil
                var connectInfo = new ConnectInfo
                {
                    IPAddr = deviceInfo.IPAddress,
                    Port = deviceInfo.Port,
                    UseSSL = deviceInfo.UseSSL
                };

                // Se connecter à l'appareil en utilisant le service de connexion
                var connectedDeviceId = _connectSvc.Connect(connectInfo);

                // Vérifier si la connexion a réussi
                if (connectedDeviceId > 0)
                {
                    // Mettre à jour le statut de connexion dans la base de données
                    deviceInfo.IsConnected = true;
                    deviceInfo.LastConnectionTime = DateTime.Now;
                    _deviceDbService.UpdateDevice(deviceInfo);

                    TempData["Message"] = $"Connexion réussie à l'appareil ID: {deviceID}";
                }
                else
                {
                    TempData["Error"] = "Échec de la connexion à l'appareil";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Erreur lors de la connexion à l'appareil: {ex.Message}";
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public IActionResult ReadLogs(int deviceID)
        {
            try
            {
                var deviceInfo = _deviceDbService.GetDeviceById(deviceID);
                if (deviceInfo == null)
                {
                    TempData["Error"] = $"Appareil avec ID {deviceID} non trouvé";
                    return RedirectToAction("Index", "Home");
                }

                // Implémentation de la lecture des logs
                // Code pour récupérer les logs...

                TempData["Message"] = $"Lecture des logs pour l'appareil ID: {deviceID}";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Erreur lors de la lecture des logs: {ex.Message}";
            }
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public IActionResult Reboot(int deviceID)
        {
            try
            {
                // Vérifier si l'appareil existe
                var deviceInfo = _deviceDbService.GetDeviceById(deviceID);
                if (deviceInfo == null)
                {
                    TempData["Error"] = $"Appareil avec ID {deviceID} non trouvé";
                    return RedirectToAction("Index", "Home");
                }

                // Convert int to uint for the API call
                uint[] deviceIDs = new uint[] { (uint)deviceID };

                // Implement reboot logic using device service
                // Exemple : _deviceService.Reboot(deviceIDs);

                TempData["Message"] = $"Redémarrage de l'appareil ID: {deviceID}";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Erreur lors du redémarrage : {ex.Message}";
            }
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public IActionResult Reset(int deviceID)
        {
            try
            {
                // Vérifier si l'appareil existe
                var deviceInfo = _deviceDbService.GetDeviceById(deviceID);
                if (deviceInfo == null)
                {
                    TempData["Error"] = $"Appareil avec ID {deviceID} non trouvé";
                    return RedirectToAction("Index", "Home");
                }

                // Implémenter la logique de réinitialisation

                TempData["Message"] = $"Réinitialisation de l'appareil ID: {deviceID}";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Erreur lors de la réinitialisation : {ex.Message}";
            }
            return RedirectToAction("Index", "Home");
        }
    }
}