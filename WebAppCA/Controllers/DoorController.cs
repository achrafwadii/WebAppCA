using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebAppCA.Models;
using WebAppCA.Services;
using WebAppCA.Data;
using Door;

namespace WebAppCA.Controllers
{
    public class DoorController : Controller
    {
        private readonly ILogger<DoorController> _logger;
        private readonly DoorService _doorService;
        private readonly DeviceService _deviceService;
        private readonly ApplicationDbContext _context;

        public DoorController(
            ILogger<DoorController> logger,
            DoorService doorService,
            DeviceService deviceService,
            ApplicationDbContext context)
        {
            _logger = logger;
            _doorService = doorService;
            _deviceService = deviceService;
            _context = context;
        }

        // GET: /Door/Index
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

                    // Récupérer les points d'accès de la base de données
                    var pointsAcces = await _context.PointsAcces.ToListAsync();

                    // Convertir les informations de porte en modèles pour la vue
                    foreach (var doorInfo in doorInfos)
                    {
                        var doorStatus = doorStatuses.FirstOrDefault(s => s.DoorID == doorInfo.DoorID);
                        var status = doorStatus != null ? (doorStatus.IsUnlocked ? "Déverrouillée" : "Verrouillée") : "Inconnu";

                        // Vérifier si cette porte existe déjà dans la base de données des points d'accès
                        var pointAcces = pointsAcces.FirstOrDefault(p => p.DoorID == doorInfo.DoorID);

                        doors.Add(new DoorInfoModel
                        {
                            DoorID = doorInfo.DoorID,
                            Name = doorInfo.Name,
                            DeviceID = doorInfo.EntryDeviceID,
                            RelayPort = (byte)doorInfo.Relay.Port,
                            DeviceName = GetDeviceName(devices, doorInfo.EntryDeviceID),
                            Status = status,
                            Description = pointAcces?.Description,
                            PointAccesId = pointAcces?.Id ?? 0
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

        // POST: /Door/AddDoor
        [HttpPost]
        public async Task<IActionResult> AddDoor(string doorName, uint deviceID, int portNumber, string description = "")
        {
            try
            {
                // Créer un identifiant unique pour la porte
                var doorID = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                // Créer l'objet DoorInfo pour l'API
                var doorInfo = DoorService.CreateDoorInfo(doorName, doorID, deviceID, (uint)portNumber);

                // Ajouter la porte via l'API
                var result = await _doorService.AddAsync(deviceID, new[] { doorInfo });

                if (result)
                {
                    // Synchroniser avec la base de données - Ajouter un point d'accès
                    var pointAcces = new PointAcces
                    {
                        DoorID = doorID,
                        Nom = doorName,
                        DeviceID = deviceID,
                        RelayPort = (byte)portNumber,
                        Description = description,
                        EstVerrouille = true, // Par défaut verrouillée
                        CreatedAt = DateTime.Now
                    };

                    _context.PointsAcces.Add(pointAcces);
                    await _context.SaveChangesAsync();

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

        // POST: /Door/ToggleDoor
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
                    bool isUnlocked = false;
                    
                    if (doorStatus != null && doorStatus.IsUnlocked)
                    {
                        // La porte est déverrouillée, donc verrouiller
                        result = await _doorService.LockAsync(deviceID, new[] { doorID });
                        if (result) TempData["Message"] = $"Porte {doorID} verrouillée avec succès";
                        isUnlocked = false;
                    }
                    else
                    {
                        // La porte est verrouillée ou état inconnu, donc déverrouiller
                        result = await _doorService.UnlockAsync(deviceID, new[] { doorID });
                        if (result) TempData["Message"] = $"Porte {doorID} déverrouillée avec succès";
                        isUnlocked = true;
                    }

                    if (result)
                    {
                        // Mettre à jour l'état dans la base de données
                        var pointAcces = await _context.PointsAcces.FirstOrDefaultAsync(p => p.DoorID == doorID);
                        if (pointAcces != null)
                        {
                            pointAcces.EstVerrouille = !isUnlocked;
                            pointAcces.UpdatedAt = DateTime.Now;
                            await _context.SaveChangesAsync();
                        }
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
                    // Pour simplifier, utilisons le premier appareil
                    var deviceID = (uint)devices[0].DeviceID;

                    // Supprimer la porte via l'API
                    var result = await _doorService.DeleteAsync(deviceID, new[] { doorID });

                    if (result)
                    {
                        // Supprimer le point d'accès correspondant
                        var pointAcces = await _context.PointsAcces.FirstOrDefaultAsync(p => p.DoorID == doorID);
                        if (pointAcces != null)
                        {
                            // Vérifier s'il existe des pointages associés à ce point d'accès
                            var hasRelatedPointages = await _context.Pointages.AnyAsync(p => p.PointAccesId == pointAcces.Id);
                            
                            if (hasRelatedPointages)
                            {
                                // Ne pas supprimer si des pointages existent, juste marquer comme inactif
                                pointAcces.UpdatedAt = DateTime.Now;
                                // Vous pourriez ajouter un champ IsActive ou Status si nécessaire
                                await _context.SaveChangesAsync();
                            }
                            else
                            {
                                // Supprimer complètement si aucun pointage
                                _context.PointsAcces.Remove(pointAcces);
                                await _context.SaveChangesAsync();
                            }
                        }

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

        // GET: /Door/DoorHistory/{doorID}
        public async Task<IActionResult> DoorHistory(int pointAccesId)
        {
            try
            {
                // Récupérer le point d'accès
                var pointAcces = await _context.PointsAcces.FirstOrDefaultAsync(p => p.Id == pointAccesId);
                
                if (pointAcces == null)
                {
                    TempData["Error"] = "Point d'accès non trouvé";
                    return RedirectToAction("Index");
                }

                // Récupérer tous les pointages liés à ce point d'accès
                var pointages = await _context.Pointages
                    .Include(p => p.Utilisateur)
                    .Where(p => p.PointAccesId == pointAccesId)
                    .OrderByDescending(p => p.DateHeure)
                    .ToListAsync();

                ViewBag.PointAcces = pointAcces;
                
                return View(pointages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement de l'historique de la porte");
                TempData["Error"] = "Une erreur est survenue: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // Méthode d'assistance pour obtenir le nom d'un appareil à partir de son ID
        private string GetDeviceName(List<DeviceInfoModel> devices, uint deviceID)
        {
            var device = devices.Find(d => d.DeviceID == deviceID);
            return device?.DeviceName ?? $"Appareil {deviceID}";
        }
    }
}