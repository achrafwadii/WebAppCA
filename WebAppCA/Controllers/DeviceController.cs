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
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using WebAppCA.Extensions;

namespace WebAppCA.Controllers
{
    public class DeviceController : Controller
    {
        private readonly ConnectSvc _connectSvc;
        private readonly DeviceDbService _deviceDbService;
        private readonly GatewayClient _gatewayClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DeviceController> _logger;

        public DeviceController(
            ConnectSvc connectSvc,
            DeviceDbService deviceDbService,
            GatewayClient gatewayClient,
            IConfiguration configuration,
            ILogger<DeviceController> logger = null)
        {
            _connectSvc = connectSvc;
            _deviceDbService = deviceDbService;
            _gatewayClient = gatewayClient;
            _configuration = configuration;
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

                // Vérifier si le service gRPC est disponible
                EnsureGrpcConnection();

                _logger?.LogInformation($"GrpcConnection établie, tentative de connexion à l'appareil");


                var connectInfo = new ConnectInfo
                {
                    IPAddr = ip,
                    Port = port,
                    UseSSL = false
                };
                System.Threading.Thread.Sleep(1000);

                uint deviceID = 0;
                try
                {
                    _logger?.LogInformation($"Appel à _connectSvc.Connect avec {ip}:{port}");
                    deviceID = _connectSvc.Connect(connectInfo);
                    _logger?.LogInformation($"Retour de _connectSvc.Connect: deviceID={deviceID}");
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
                                DeviceId = (int)deviceID, // Make sure ID is set properly
                                DeviceName = $"Device-{deviceID}",
                                IPAddress = ip ?? "unknown", // Prevent nulls
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
                            // Ensure no properties are null before updating
                            existingDevice.IPAddress = ip ?? existingDevice.IPAddress ?? "unknown";
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
            catch (InvalidOperationException ex) when (ex.Message.Contains("client gRPC n'est pas initialisé"))
            {
                _logger?.LogError(ex, "Erreur de connexion gRPC: {Message}", ex.Message);
                TempData["Error"] = "Le service de connexion gRPC n'est pas disponible. Vérifiez que le serveur gRPC est en cours d'exécution.";
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

                // Vérifier si le service gRPC est disponible
                EnsureGrpcConnection();

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
            catch (InvalidOperationException ex) when (ex.Message.Contains("client gRPC n'est pas initialisé"))
            {
                _logger?.LogError(ex, "Erreur de connexion gRPC: {Message}", ex.Message);
                TempData["Error"] = "Le service de connexion gRPC n'est pas disponible. Vérifiez que le serveur gRPC est en cours d'exécution.";
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Exception non gérée lors de la connexion à l'appareil {deviceID}: {ex.Message}");
                TempData["Error"] = $"Erreur lors de la connexion à l'appareil: {ex.Message}";
            }

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// S'assure que la connexion gRPC est active, essaie de la rétablir si nécessaire
        /// </summary>
        private void EnsureGrpcConnection()
        {
            // Vérifier si _connectSvc a une méthode IsAvailable (dans la version améliorée)
            var connectSvcType = _connectSvc.GetType();
            var isAvailableMethod = connectSvcType.GetMethod("IsAvailable");

            if (isAvailableMethod != null)
            {
                bool isAvailable = (bool)isAvailableMethod.Invoke(_connectSvc, null);
                if (!isAvailable)
                {
                    _logger?.LogWarning("Le service ConnectSvc n'est pas disponible, tentative de reconnexion");

                    // Tenter de reconnecter le GatewayClient
                    var certPath = _configuration.GetValue<string>("GrpcSettings:CaCertPath") ?? "";
                    var address = _configuration.GetValue<string>("GrpcSettings:Address") ?? "localhost";
                    var port = _configuration.GetValue<int>("GrpcSettings:Port", 51211);

                    bool reconnected = _gatewayClient.Connect(certPath, address, port);

                    if (reconnected)
                    {
                        _logger?.LogInformation("Reconnexion du GatewayClient réussie");

                        // Maintenant, essayer de réinitialiser ConnectSvc avec le nouveau canal
                        var tryReconnectMethod = connectSvcType.GetMethod("TryReconnect");
                        if (tryReconnectMethod != null)
                        {
                            bool success = (bool)tryReconnectMethod.Invoke(_connectSvc, new object[] { _gatewayClient.Channel });
                            if (!success)
                            {
                                _logger?.LogError("Échec de la réinitialisation de ConnectSvc");
                                throw new InvalidOperationException("Le client gRPC n'est pas initialisé. Assurez-vous que le canal (channel) est correctement configuré.");
                            }
                        }
                    }
                    else
                    {
                        _logger?.LogError("Échec de la reconnexion du GatewayClient");
                        throw new InvalidOperationException("Le client gRPC n'est pas initialisé. Assurez-vous que le canal (channel) est correctement configuré.");
                    }
                }
            }
        } 
        // Les autres méthodes restent similaires, avec l'ajout de journalisation appropriée
    }
}