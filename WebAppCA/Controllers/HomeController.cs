using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WebAppCA.Models;
using WebAppCA.Services;
using System.Linq;
using System.Collections.Generic;

namespace WebAppCA.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly DeviceDbService _deviceService;
        private readonly ConnectSvc _connectSvc;

        public HomeController(
            ILogger<HomeController> logger,
            DeviceDbService deviceService,
            ConnectSvc connectSvc)
        {
            _logger = logger;
            _deviceService = deviceService;
            _connectSvc = connectSvc;
        }

        public IActionResult Index()
        {
            // Get devices from database
            var dbDevices = _deviceService.GetAllDevices();

            // Try to get connected devices from gRPC service
            var deviceInfoModels = new List<DeviceInfoModel>();

            try
            {
                var connectedDevices = _connectSvc.GetDeviceList();

                // Create view models by combining DB data with connected device status
                foreach (var device in dbDevices)
                {
                    // Assuming the DeviceId from our DB matches the DeviceID from gRPC service
                    // Note: Property names might differ between your local and gRPC models
                    var connectedDevice = connectedDevices.FirstOrDefault(d => d.DeviceID.ToString() == device.DeviceId.ToString());

                    deviceInfoModels.Add(new DeviceInfoModel
                    {
                        DeviceID = device.DeviceId,
                        DeviceName = device.Name,
                        IPAddress = device.IPAddress,
                        ConnectionStatus = connectedDevice != null ? "Connecté" : "Déconnecté"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving connected devices");

                // If gRPC service fails, still display devices from DB
                foreach (var device in dbDevices)
                {
                    deviceInfoModels.Add(new DeviceInfoModel
                    {
                        DeviceID = device.DeviceId,
                        DeviceName = device.Name,
                        IPAddress = device.IPAddress,
                        ConnectionStatus = "Statut inconnu"
                    });
                }

                // Add message to notify user
                TempData["ErrorMessage"] = "Impossible de se connecter au service gRPC";
            }

            return View(deviceInfoModels);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        public IActionResult About()
        {
            return View();
        }

        public IActionResult Help()
        {
            return View();
        }

        public IActionResult Dashboard()
        {
            return View();
        }

        public IActionResult Attendance()
        {
            return View();
        }

        public IActionResult Reports()
        {
            return View();
        }



        public IActionResult Doors()
        {
            // Redirect to DoorController.Index (note the singular Door)
            return RedirectToAction("Index", "Door");
        }

        public IActionResult Welcome()
        {
            // Cette action affichera la page d'accueil sans vérifier l'authentification
            return View();
        }

    }
}