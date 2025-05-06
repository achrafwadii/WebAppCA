using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using WebAppCA.Models;
using WebAppCA.Services;

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

        // GET: /Door/Index
        public async Task<IActionResult> Index()
        {
            // Ajouter des appareils de test si la base est vide (pour la démo)
            await _deviceService.AddTestDevicesIfEmptyAsync();

            // Récupérer la liste des portes (simulée pour l'exemple)
            var doors = new List<DoorInfoModel>();

            // Récupérer tous les appareils pour alimenter le dropdown
            var devices = await _deviceService.GetAllDevicesAsync();
            
            // Si nous avons au moins un appareil, essayons de récupérer ses portes
            if (devices.Count > 0)
            {
                doors = await _doorService.GetDoorsAsync(devices[0].DeviceID);
            }

            // Passer les appareils à la vue
            ViewBag.Devices = devices;

            return View(doors);
        }

        // POST: /Door/AddDoor
        [HttpPost]
        public async Task<IActionResult> AddDoor(string doorName, uint deviceID, int portNumber)
        {
            try
            {
                var model = new AddDoorModel
                {
                    DoorName = doorName,
                    DeviceID = deviceID,
                    PortNumber = portNumber
                };

                var result = await _doorService.AddDoorAsync(model);

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
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("Index");
        }

        // POST: /Door/ToggleDoor
        [HttpPost]
        public async Task<IActionResult> ToggleDoor(uint doorID)
        {
            try
            {
                // Récupérer les appareils pour trouver celui qui contient cette porte
                var devices = await _deviceService.GetAllDevicesAsync();
                
                if (devices.Count > 0)
                {
                    // Pour la simplicité, supposons que la porte est sur le premier appareil
                    var deviceID = (uint)devices[0].DeviceID;
                    
                    // Supposons que la porte est actuellement verrouillée (pour la démo)
                    // Dans un cas réel, nous devrions vérifier l'état actuel
                    var unlock = true;
                    
                    var result = await _doorService.ToggleDoorAsync(deviceID, doorID, unlock);
                    
                    if (result)
                    {
                        TempData["Message"] = $"État de la porte {doorID} modifié avec succès";
                    }
                    else
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

        // POST: /Door/DeleteDoor
        [HttpPost]
        public async Task<IActionResult> DeleteDoor(uint doorID)
        {
            try
            {
                // Récupérer les appareils pour trouver celui qui contient cette porte
                var devices = await _deviceService.GetAllDevicesAsync();
                
                if (devices.Count > 0)
                {
                    // Pour la simplicité, supposons que la porte est sur le premier appareil
                    var deviceID = (uint)devices[0].DeviceID;
                    
                    var result = await _doorService.DeleteDoorAsync(deviceID, doorID);
                    
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
    }
}