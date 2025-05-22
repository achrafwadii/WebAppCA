using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO.Ports;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using WebAppCA.Models;

namespace WebAppCA.Controllers
{
    public class MenuController : Controller
    {
        private readonly ILogger<MenuController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHostApplicationLifetime _applicationLifetime;
        private readonly string _usersFilePath;
        private readonly IWebHostEnvironment _env;

        public MenuController(
            ILogger<MenuController> logger,
            IConfiguration configuration,
            IHostApplicationLifetime applicationLifetime,
            IWebHostEnvironment env)
        {
            _logger = logger;
            _configuration = configuration;
            _applicationLifetime = applicationLifetime;
            _env = env;
            _usersFilePath = Path.Combine(env.ContentRootPath, "users.json");
        }

        // Actions principales
        public IActionResult Index() => View();

        [HttpGet]
        public IActionResult SystemSettings()
        {
            var settings = new SystemSettingsViewModel
            {
                DeviceName = _configuration["DeviceSettings:Name"],
                TimeZone = _configuration["DeviceSettings:TimeZone"],
                Language = _configuration["DeviceSettings:Language"],
                AutoLockTimeout = int.Parse(_configuration["DeviceSettings:AutoLockTimeout"] ?? "300")
            };
            return View(settings);
        }

        [HttpPost]
        public async Task<IActionResult> SystemSettings(SystemSettingsViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await SaveSystemSettingsAsync(model);
                    TempData["SuccessMessage"] = "Paramètres enregistrés avec succès";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur sauvegarde paramètres");
                    TempData["ErrorMessage"] = "Erreur lors de la sauvegarde";
                }
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult Ports()
        {
            var model = new PortsViewModel
            {
                AvailablePorts = SerialPort.GetPortNames().ToList(),
                HttpPort = int.Parse(_configuration["Ports:Http"] ?? "5000"),
                HttpsPort = int.Parse(_configuration["Ports:Https"] ?? "5001")
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Ports(PortsViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await SavePortSettingsAsync(model);
                    TempData["SuccessMessage"] = "Ports configurés avec succès";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur configuration ports");
                    TempData["ErrorMessage"] = "Erreur de configuration";
                }
            }
            return View(model);
        }

        public IActionResult About()
        {
            var model = new AboutViewModel
            {
                SystemVersion = _configuration["Version:System"],
                FirmwareVersion = _configuration["Version:Firmware"],
                SerialNumber = _configuration["Device:Serial"],
                InstallDate = DateTime.Parse(_configuration["Device:InstallDate"] ?? DateTime.Now.ToString())
            };
            return View(model);
        }

        // Gestion des comptes
        [HttpGet]
        public IActionResult DeleteAccount()
        {
            var currentUser = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(currentUser))
                return RedirectToAction("Login", "Account");

            return View(new DeleteAccountViewModel { Username = currentUser });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteAccount(DeleteAccountViewModel model)
        {
            try
            {
                var currentUser = HttpContext.Session.GetString("Username");
                if (string.IsNullOrEmpty(currentUser))
                    return RedirectToAction("Login", "Account");

                var users = LoadUsers();
                var user = users.FirstOrDefault(u => u.Username == currentUser);

                if (user == null || !VerifyPassword(model.Password, user.PasswordHash))
                {
                    TempData["ErrorMessage"] = "Mot de passe incorrect";
                    return View(model);
                }

                users.RemoveAll(u => u.Username == currentUser);
                SaveUsers(users);

                HttpContext.Session.Clear();
                Response.Cookies.Delete(".AspNetCore.Session");

                TempData["SuccessMessage"] = "Compte supprimé avec succès";
                return RedirectToAction("Login", "Account");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur suppression compte");
                TempData["ErrorMessage"] = "Erreur critique lors de la suppression";
                return View(model);
            }
        }

        // Fonctions système
        [HttpPost]
        public async Task<IActionResult> BackupSystem()
        {
            try
            {
                var backupPath = await PerformSystemBackupAsync();
                return Json(new { success = true, path = backupPath });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur sauvegarde");
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult RestartSystem()
        {
            try
            {
                Task.Run(async () =>
                {
                    await Task.Delay(1000);
                    _applicationLifetime.StopApplication();
                });
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur redémarrage");
                return Json(new { success = false });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CheckForUpdates()
        {
            try
            {
                var updateInfo = await CheckUpdatesAsync();
                return Json(updateInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur vérification MAJ");
                return Json(new { success = false });
            }
        }

        #region Helpers
        private List<Useer> LoadUsers()
        {
            if (!System.IO.File.Exists(_usersFilePath))
                return new List<Useer>();

            var json = System.IO.File.ReadAllText(_usersFilePath);
            return JsonSerializer.Deserialize<List<Useer>>(json) ?? new List<Useer>();
        }

        private void SaveUsers(List<Useer> users)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(users, options);
            System.IO.File.WriteAllText(_usersFilePath, json);
        }

        private bool VerifyPassword(string password, string storedHash)
        {
            using var sha = SHA256.Create();
            var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            return BitConverter.ToString(hashBytes) == storedHash;
        }

        private async Task SaveSystemSettingsAsync(SystemSettingsViewModel model)
        {
            // Implémentez la logique de sauvegarde réelle ici
            await Task.Delay(500);
        }

        private async Task SavePortSettingsAsync(PortsViewModel model)
        {
            // Implémentez la logique de sauvegarde des ports ici
            await Task.Delay(500);
        }

        private async Task<string> PerformSystemBackupAsync()
        {
            // Logique de sauvegarde
            await Task.Delay(2000);
            return "/backups/backup.zip";
        }

        private async Task<UpdateInfo> CheckUpdatesAsync()
        {
            // Logique de vérification MAJ
            await Task.Delay(1000);
            return new UpdateInfo { UpdateAvailable = false };
        }
        #endregion
    }

    public class UpdateInfo
    {
        public bool UpdateAvailable { get; set; }
        public string CurrentVersion { get; set; }
        public string NewVersion { get; set; }
    }
}