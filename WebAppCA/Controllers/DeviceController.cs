using Microsoft.AspNetCore.Mvc;
using WebAppCA.Models;

namespace WebAppCA.Controllers
{
    public class DeviceController : Controller
    {
        [HttpPost]
        public IActionResult Connect(string ip, int port)
        {
            // Exemple de traitement (tu peux adapter)
            if (string.IsNullOrEmpty(ip) || port == 0)
                return BadRequest("IP ou port invalide");

            // Simule un device connecté
            TempData["Message"] = $"Équipement connecté : {ip}:{port}";
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public IActionResult Connect(int deviceID)
        {
            // Connexion via ID depuis le tableau
            TempData["Message"] = $"Connexion à l'appareil ID: {deviceID}";
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public IActionResult ReadLogs(int deviceID)
        {
            TempData["Message"] = $"Lecture des logs pour l'appareil ID: {deviceID}";
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public IActionResult Reboot(int deviceID)
        {
            TempData["Message"] = $"Redémarrage de l'appareil ID: {deviceID}";
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public IActionResult Reset(int deviceID)
        {
            TempData["Message"] = $"Réinitialisation de l'appareil ID: {deviceID}";
            return RedirectToAction("Index", "Home");
        }
        // Pour le formulaire avec IP et port (ajout d'un device)
        [HttpPost]
        public IActionResult ConnectByIP(string ip, int port)
        {
            TempData["Message"] = $"Équipement connecté : {ip}:{port}";
            return RedirectToAction("Index", "Home");
        }

    }
}
