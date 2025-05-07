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
        // Dans HomeController.cs, ajouter cette méthode mise à jour

        private readonly DashboardService _dashboardService;

        // Modifier le constructeur pour injecter le DashboardService
        public HomeController(
            ILogger<HomeController> logger,
            DeviceDbService deviceService,
            ConnectSvc connectSvc,
            DashboardService dashboardService)
        {
            _logger = logger;
            _deviceService = deviceService;
            _connectSvc = connectSvc;
            _dashboardService = dashboardService;
        }

        public async Task<IActionResult> Dashboard(string timeFrame = "today")
        {
            try
            {
                // Valider le timeFrame
                if (timeFrame != "today" && timeFrame != "week" && timeFrame != "month")
                {
                    timeFrame = "today";
                }

                // Récupérer les données du tableau de bord
                var dashboardData = await _dashboardService.GetDashboardDataAsync(timeFrame);

                // Définir le timeFrame actif pour l'affichage dans la vue
                ViewBag.ActiveTimeFrame = timeFrame;

                return View(dashboardData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement du tableau de bord");
                TempData["Error"] = "Une erreur s'est produite lors du chargement du tableau de bord.";
                return View(new DashboardViewModel());
            }
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
                        DeviceID = (int)device.DeviceID,
                        DeviceName = device.DeviceName,
                        IPAddress = device.IPAddress,
                        Status = connectedDevice != null ? "Connecté" : "Déconnecté"
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
                        DeviceID = (int)device.DeviceID,
                        DeviceName = device.DeviceName,
                        IPAddress = device.IPAddress,
                        Status = "Statut inconnu"
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

        public IActionResult Attendance()
        {
            return View();
        }

        public IActionResult Reports()
        {
            return View();
        }




        public IActionResult Welcome()
        {
            // Cette action affichera la page d'accueil sans vérifier l'authentification
            return View();
        }

    }
}