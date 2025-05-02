// WebAppCA/Controllers/DeviceController.cs
using Microsoft.AspNetCore.Mvc;
using WebAppCA.Models;
using WebAppCA.Services;
using Connect;
using System;
using System.Threading.Tasks;
using ConnectInfo = Connect.ConnectInfo;
using DbDeviceInfo = WebAppCA.Models.DeviceInfo;
using Microsoft.Extensions.Logging;

namespace WebAppCA.Controllers
{
    public class DeviceController : Controller
    {
        private readonly ConnectSvc _connectSvc;
        private readonly DeviceDbService _deviceDbService;
        private readonly ILogger<DeviceController> _logger;

        public DeviceController(
            ConnectSvc connectSvc, 
            DeviceDbService deviceDbService,
            ILogger<DeviceController> logger = null)
        {
            _connectSvc = connectSvc;
            _deviceDbService = deviceDbService;
            _logger = logger;
        }

        [HttpPost]
        public IActionResult ConnectByIPAndPort(string ip, int port)
        {
            if (string.IsNullOrEmpty(ip) || port <= 0)
            {
                TempData["Error"] = "IP ou port invalide";
                return RedirectToAction("Index", "Home");
            }

            try
            {
                _logger?.LogInformation($"Tentative de connexion à {ip}:{port}");

                var connectInfo = new ConnectInfo
                {
                    IPAddr = ip,
                    Port = port,
                    UseSSL = false
                };

                uint deviceID = 0;
                try
                {
                    deviceID = _connectSvc.Connect(connectInfo);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, $"Erreur lors de l'appel à Connect: {ex.Message}");
                    TempData["Error"] = $"Erreur de connexion : {ex.Message}";
                    return RedirectToAction("Index", "Home");
                }

                if (deviceID > 0)
                {
                    _logger?.LogInformation($"Connexion réussie. DeviceID: {deviceID}");
                    
                    // Création ou mise à jour de l'appareil dans la base de données
                    try 
                    {
                        var existingDevice = _deviceDbService.GetDeviceById((int)deviceID);
                        
                        if (existingDevice == null)
                        {
                            _logger?.LogInformation($"Création d'un nouvel appareil avec ID: {deviceID}");
                            var newDevice = new DbDeviceInfo
                            {
                                Name = $"Device-{deviceID}",
                                IPAddress = ip,
                                Port = port,
                                UseSSL = false,
                                Description = "Ajouté automatiquement",
                                LastConnectionTime = DateTime.Now,
                                IsConnected = true,
                                Status = "Connecté"
                            };
                            _deviceDbService.AddDevice(newDevice);
                        }
                        else
                        {
                            _logger?.LogInformation($"Mise à jour de l'appareil existant avec ID: {deviceID}");
                            existingDevice.IPAddress = ip;
                            existingDevice.Port = port;
                            existingDevice.LastConnectionTime = DateTime.Now;
                            existingDevice.IsConnected = true;
                            existingDevice.Status = "Connecté";
                            _deviceDbService.UpdateDevice(existingDevice);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, $"Erreur lors de la mise à jour de la base de données: {ex.Message}");
                        // Ne pas interrompre le flux si la BDD échoue
                    }

                    TempData["Message"] = $"Équipement connecté : {ip}:{port} (ID: {deviceID})";
                }
                else
                {
                    _logger?.LogWarning($"Échec de connexion à {ip}:{port}. DeviceID retourné: {deviceID}");
                    TempData["Error"] = "Échec de la connexion : ID de dispositif invalide retourné";
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Exception non gérée: {ex.Message}");
                TempData["Error"] = $"Erreur de connexion : {ex.Message}";
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public IActionResult ConnectByDeviceID(int deviceID)
        {
            if (deviceID <= 0)
            {
                TempData["Error"] = "ID d'appareil invalide";
                return RedirectToAction("Index", "Home");
            }

            try
            {
                _logger?.LogInformation($"Tentative de connexion par ID: {deviceID}");
                
                // Récupérer les informations de l'appareil depuis la base de données
                var deviceInfo = _deviceDbService.GetDeviceById(deviceID);

                if (deviceInfo == null)
                {
                    _logger?.LogWarning($"Appareil avec ID {deviceID} non trouvé dans la base de données");
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

                uint connectedDeviceId = 0;
                try 
                {
                    // Se connecter à l'appareil en utilisant le service de connexion
                    connectedDeviceId = _connectSvc.Connect(connectInfo);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, $"Erreur lors de l'appel à Connect pour deviceID={deviceID}: {ex.Message}");
                    TempData["Error"] = $"Erreur de connexion : {ex.Message}";
                    return RedirectToAction("Index", "Home");
                }

                // Vérifier si la connexion a réussi
                if (connectedDeviceId > 0)
                {
                    _logger?.LogInformation($"Connexion réussie à l'appareil ID: {deviceID}");
                    
                    try
                    {
                        // Mettre à jour le statut de connexion dans la base de données
                        deviceInfo.IsConnected = true;
                        deviceInfo.LastConnectionTime = DateTime.Now;
                        deviceInfo.Status = "Connecté";
                        _deviceDbService.UpdateDevice(deviceInfo);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, $"Erreur lors de la mise à jour de l'état de connexion dans la base de données: {ex.Message}");
                        // Ne pas interrompre le flux si la BDD échoue
                    }

                    TempData["Message"] = $"Connexion réussie à l'appareil ID: {deviceID}";
                }
                else
                {
                    _logger?.LogWarning($"Échec de la connexion à l'appareil ID: {deviceID}. ID retourné: {connectedDeviceId}");
                    TempData["Error"] = "Échec de la connexion à l'appareil";
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Exception non gérée lors de la connexion à l'appareil {deviceID}: {ex.Message}");
                TempData["Error"] = $"Erreur lors de la connexion à l'appareil: {ex.Message}";
            }

            return RedirectToAction("Index", "Home");
        }

        // Les autres méthodes restent similaires, avec l'ajout de journalisation appropriée
    }
}