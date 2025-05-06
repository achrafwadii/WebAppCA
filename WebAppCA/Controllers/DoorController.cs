using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebAppCA.Models;
using WebAppCA.Services;
using Door;

namespace WebAppCA.Controllers
{
    public class DoorController : Controller
    {
        private readonly ILogger<DoorController> _logger;
        private readonly DoorService _doorService;
        private readonly DeviceService _deviceService;

        public DoorController(
            ILogger<DoorController> logger,
            DoorService doorService,
            DeviceService deviceService)
        {
            _logger = logger;
            _doorService = doorService;
            _deviceService = deviceService;
        }

        // GET: /GsdkDoor/Index
        public async Task<IActionResult> Index()
        {
            try
            {
                // Ajouter des appareils de test si la base est vide (pour la démo)
                await _deviceService.AddTestDevicesIfEmptyAsync();

                // Récupérer tous les appareils pour alimenter le dropdown
                var devices = await _deviceService.GetAllDevicesAsync();

                // Préparer la liste des portes
                var doors = new List<DoorInfoModel>();

                // Si nous avons au moins un appareil, récupérer ses portes
                if (devices.Count > 0)
                {
                    var deviceID = (uint)devices[0].DeviceID;
                    var doorInfos = await _doorService.GetListAsync(deviceID);
                    var doorStatuses = await _doorService.GetStatusAsync(deviceID);

                    // Convertir les informations de porte en modèles pour la vue
                    foreach (var doorInfo in doorInfos)
                    {
                        var doorStatus = doorStatuses.FirstOrDefault(s => s.DoorID == doorInfo.DoorID);
                        var status = doorStatus != null ? (doorStatus.IsUnlocked ? "Déverrouillée" : "Verrouillée") : "Inconnu";

                        doors.Add(new DoorInfoModel
                        {
                            DoorID = doorInfo.DoorID,
                            Name = doorInfo.Name,
                            DeviceID = doorInfo.EntryDeviceID,
                            RelayPort = (byte)doorInfo.Relay.Port,
                            DeviceName = GetDeviceName(devices, doorInfo.EntryDeviceID),
                            Status = status
                        });
                    }
                }

                // Passer les appareils à la vue
                ViewBag.Devices = devices;

                return View(doors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement de la page Index");
                TempData["Error"] = "Une erreur est survenue lors du chargement des portes: " + ex.Message;
                return View(new List<DoorInfoModel>());
            }
        }

        // POST: /GsdkDoor/AddDoor
        [HttpPost]
        public async Task<IActionResult> AddDoor(string doorName, uint deviceID, int portNumber)
        {
            try
            {
                // Créer un identifiant unique pour la porte
                var doorID = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                // Créer l'objet DoorInfo
                var doorInfo = DoorService.CreateDoorInfo(doorName, doorID, deviceID, (uint)portNumber);

                // Ajouter la porte
                var result = await _doorService.AddAsync(deviceID, new[] { doorInfo });

                if (result)
                {
                    TempData["Message"] = "Porte ajoutée avec succès";
                }
                else
                {
                    TempData["Error"] = "Erreur lors de l'ajout de la porte";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'ajout de la porte");
                TempData["Error"] = "Erreur lors de l'ajout de la porte: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // POST: /GsdkDoor/ToggleDoor
        [HttpPost]
        public async Task<IActionResult> ToggleDoor(uint doorID, bool unlock = true)
        {
            try
            {
                // Récupérer les appareils pour trouver celui qui contient cette porte
                var devices = await _deviceService.GetAllDevicesAsync();

                if (devices.Count > 0)
                {
                    // Pour simplifier, utilisons le premier appareil
                    var deviceID = (uint)devices[0].DeviceID;

                    // Vérifier l'état actuel de la porte
                    var doorStatuses = await _doorService.GetStatusAsync(deviceID);
                    var doorStatus = doorStatuses.FirstOrDefault(s => s.DoorID == doorID);

                    // Déterminer l'action à effectuer
                    bool result;
                    if (doorStatus != null && doorStatus.IsUnlocked)
                    {
                        // La porte est déverrouillée, donc verrouiller
                        result = await _doorService.LockAsync(deviceID, new[] { doorID });
                        if (result) TempData["Message"] = $"Porte {doorID} verrouillée avec succès";
                    }
                    else
                    {
                        // La porte est verrouillée ou état inconnu, donc déverrouiller
                        result = await _doorService.UnlockAsync(deviceID, new[] { doorID });
                        if (result) TempData["Message"] = $"Porte {doorID} déverrouillée avec succès";
                    }

                    if (!result)
                    {
                        TempData["Error"] = $"Erreur lors du changement d'état de la porte {doorID}";
                    }
                }
                else
                {
                    TempData["Error"] = "Aucun appareil disponible pour contrôler cette porte";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors du changement d'état de la porte {doorID}");
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("Index");
        }

        // POST: /GsdkDoor/DeleteDoor
        [HttpPost]
        public async Task<IActionResult> DeleteDoor(uint doorID)
        {
            try
            {
                // Récupérer les appareils pour trouver celui qui contient cette porte
                var devices = await _deviceService.GetAllDevicesAsync();

                if (devices.Count > 0)
                {
                    // Pour simplifier, utilisons le premier appareil
                    var deviceID = (uint)devices[0].DeviceID;

                    // Supprimer la porte
                    var result = await _doorService.DeleteAsync(deviceID, new[] { doorID });

                    if (result)
                    {
                        TempData["Message"] = $"Porte {doorID} supprimée avec succès";
                    }
                    else
                    {
                        TempData["Error"] = $"Erreur lors de la suppression de la porte {doorID}";
                    }
                }
                else
                {
                    TempData["Error"] = "Aucun appareil disponible pour supprimer cette porte";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la suppression de la porte {doorID}");
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("Index");
        }

        // Méthode d'assistance pour obtenir le nom d'un appareil à partir de son ID
        private string GetDeviceName(List<DeviceInfoModel> devices, uint deviceID)
        {
            var device = devices.Find(d => d.DeviceID == deviceID);
            return device?.DeviceName ?? $"Appareil {deviceID}";
        }
    }
}