// WebAppCA/Controllers/DeviceController.cs
using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using connect;                               // pour Connect.ConnectClient, ConnectRequest, etc.
using GrpcConnectInfo = connect.ConnectInfo; // alias pour la classe gRPC
using LocalConnectInfo = WebAppCA.Models.ConnectInfo; // alias pour votre DTO
using WebAppCA.Models;
using WebAppCA.Services;
using DbDeviceInfo = WebAppCA.Models.DeviceInfo;

namespace WebAppCA.Controllers
{
    public class DeviceController : Controller
    {
        readonly ConnectSvc _connectSvc;
        readonly DeviceDbService _deviceDbService;
        readonly GatewayClient _gatewayClient;
        readonly IConfiguration _configuration;
        readonly ILogger<DeviceController> _logger;

        public DeviceController(
            ConnectSvc connectSvc,
            DeviceDbService deviceDbService,
            GatewayClient gatewayClient,
            IConfiguration configuration,
            ILogger<DeviceController> logger)
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

                var grpcInfo = new GrpcConnectInfo
                {
                    IPAddr = ip,
                    Port = port,
                    UseSSL = false
                };

                uint deviceID = _connectSvc.Connect(grpcInfo);

                if (deviceID <= 0)
                {
                    TempData["Error"] = "Échec de la connexion : ID appareil invalide";
                    return RedirectToAction("Index", "Home");
                }

                UpsertDeviceRecord(deviceID, ip, port, false);

                TempData["Message"] = $"Équipement connecté : {ip}:{port} (ID : {deviceID})";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur de connexion à {IP}:{Port}", ip, port);
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

                var dbInfo = _deviceDbService.GetDeviceById(deviceID);
                if (dbInfo == null)
                {
                    TempData["Error"] = $"Appareil ID {deviceID} introuvable";
                    return RedirectToAction("Index", "Home");
                }

                var grpcInfo = new GrpcConnectInfo
                {
                    IPAddr = dbInfo.IPAddress,
                    Port = dbInfo.Port,
                    UseSSL = dbInfo.UseSSL
                };

                uint connectedId = _connectSvc.Connect(grpcInfo);
                if (connectedId <= 0)
                {
                    TempData["Error"] = "Échec de la connexion à l'appareil";
                    return RedirectToAction("Index", "Home");
                }

                dbInfo.IsConnected = true;
                dbInfo.LastConnectionTime = DateTime.Now;
                dbInfo.Status = "Connecté";
                _deviceDbService.UpdateDevice(dbInfo);

                TempData["Message"] = $"Connexion réussie à l'appareil ID : {deviceID}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur de connexion à l'appareil ID {DeviceID}", deviceID);
                TempData["Error"] = $"Erreur : {ex.Message}";
            }

            return RedirectToAction("Index", "Home");
        }
        void EnsureGrpcConnection()
        {
            if (_connectSvc.IsConnected)
                return;

            _logger?.LogWarning("ConnectSvc non connecté, tentative de reconnexion");

            var caPath = _configuration["GrpcSettings:CaCertPath"];
            var address = _configuration["GrpcSettings:Address"];
            var port = _configuration.GetValue<int>("GrpcSettings:Port");
            var useSSL = _configuration.GetValue<bool>("GrpcSettings:UseSSL", false);

            bool connected = false;
            if (useSSL && !string.IsNullOrEmpty(caPath) && System.IO.File.Exists(caPath))
            {
                connected = _gatewayClient.ConnectSecure(address, port, true).GetAwaiter().GetResult();
            }
            else
            {
                connected = _gatewayClient.Connect(address, port);
            }

            if (!connected)
                throw new InvalidOperationException("Impossible de reconnecter le canal gRPC");

            if (!_connectSvc.TryReconnectAsync().GetAwaiter().GetResult())
                throw new InvalidOperationException("Impossible de reconnecter ConnectSvc");
        }
        void UpsertDeviceRecord(uint deviceID, string ip, int port, bool useSsl)
        {
            var existing = _deviceDbService.GetDeviceById((int)deviceID);

            if (existing == null)
            {
                var newDev = new DbDeviceInfo
                {
                    DeviceId = (int)deviceID,
                    DeviceName = $"Device-{deviceID}",
                    IPAddress = ip,
                    Port = port,
                    UseSSL = useSsl,
                    Description = "Ajout automatique",
                    LastConnectionTime = DateTime.Now,
                    IsConnected = true,
                    Status = "Connecté"
                };
                _deviceDbService.AddDevice(newDev);
            }
            else
            {
                existing.IPAddress = ip;
                existing.Port = port;
                existing.LastConnectionTime = DateTime.Now;
                existing.IsConnected = true;
                existing.Status = "Connecté";
                _deviceDbService.UpdateDevice(existing);
            }
        }
    }
}
