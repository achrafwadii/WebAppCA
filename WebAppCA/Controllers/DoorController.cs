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
using Google.Protobuf.Collections;

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
        public async Task<IActionResult> Index(uint? selectedDeviceId = null)
        {
            try
            {
                // Ajouter des appareils de test si la base est vide (pour la démo)
                await _deviceService.AddTestDevicesIfEmptyAsync();

                // Récupérer tous les appareils pour alimenter le dropdown
                var devices = await _deviceService.GetAllDevicesAsync();

                // Vérifier si devices est null
                if (devices == null)
                {
                    _logger.LogWarning("La liste des appareils est null");
                    devices = new List<DeviceInfoModel>();
                }

                // Préparer la liste des portes
                var doors = new List<DoorInfoModel>();

                // Si nous avons au moins un appareil, récupérer ses portes
                if (devices.Count > 0)
                {
                    // Utiliser l'appareil sélectionné ou le premier par défaut
                    var deviceID = selectedDeviceId ?? (uint)devices[0].DeviceID;

                    // Stocker l'ID sélectionné pour la vue
                    ViewBag.SelectedDeviceId = deviceID;

                    try
                    {
                        var doorInfos = await _doorService.GetListAsync(deviceID);

                        // Vérifier si doorInfos est null
                        if (doorInfos == null)
                        {
                            _logger.LogWarning("La liste des informations de portes est null pour l'appareil {DeviceID}", deviceID);
                            doorInfos = new RepeatedField<DoorInfo>();
                        }

                        var doorStatuses = await _doorService.GetStatusAsync(deviceID);

                        // Vérifier si doorStatuses est null
                        if (doorStatuses == null)
                        {
                            _logger.LogWarning("La liste des statuts de portes est null pour l'appareil {DeviceID}", deviceID);
                            doorStatuses = new RepeatedField<Door.Status>();
                        }

                        // Récupérer les points d'accès de la base de données
                        var pointsAcces = await _context.PointsAcces.ToListAsync();

                        // S'assurer que pointsAcces n'est pas null
                        if (pointsAcces == null)
                        {
                            _logger.LogWarning("La liste des points d'accès est null");
                            pointsAcces = new List<PointAcces>();
                        }

                        // Convertir les informations de porte en modèles pour la vue
                        foreach (var doorInfo in doorInfos)
                        {
                            if (doorInfo == null) continue; // Ignorer les éléments null

                            var doorStatus = doorStatuses?.FirstOrDefault(s => s != null && s.DoorID == doorInfo.DoorID);
                            var status = doorStatus != null ? (doorStatus.IsUnlocked ? "Déverrouillée" : "Verrouillée") : "Inconnu";

                            // Vérifier si cette porte existe déjà dans la base de données des points d'accès
                            var pointAcces = pointsAcces.FirstOrDefault(p => p != null && p.DoorID == doorInfo.DoorID);

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
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erreur lors de la récupération des données de porte pour l'appareil {DeviceID}", deviceID);
                        // Continuer avec une liste vide, mais ne pas faire échouer toute la méthode
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
        // POST: /Door/FilterByDevice
        [HttpPost]
        public IActionResult FilterByDevice(uint deviceId)
        {
            return RedirectToAction("Index", new { selectedDeviceId = deviceId });
        }

        // POST: /Door/AddDoor
        [HttpPost]
        public async Task<IActionResult> AddDoor(AddDoorModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["Error"] = "Données de formulaire invalides";
                    return RedirectToAction("Index");
                }

                // Créer un identifiant unique pour la porte
                var doorID = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                // Créer l'objet DoorInfo pour l'API
                var doorInfo = DoorService.CreateDoorInfo(model.DoorName, doorID, model.DeviceID, (uint)model.PortNumber);

                // Ajouter la porte via l'API
                var result = await _doorService.AddAsync(model.DeviceID, new[] { doorInfo });

                if (result)
                {
                    // Synchroniser avec la base de données - Ajouter un point d'accès
                    var pointAcces = new PointAcces
                    {
                        DoorID = doorID,
                        Nom = model.DoorName,
                        DeviceID = model.DeviceID,
                        RelayPort = (byte)model.PortNumber,
                        Description = model.Description,
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
                // Récupérer le point d'accès pour connaître son device ID
                var pointAcces = await _context.PointsAcces.FirstOrDefaultAsync(p => p.DoorID == doorID);

                if (pointAcces == null)
                {
                    TempData["Error"] = $"Point d'accès non trouvé pour la porte {doorID}";
                    return RedirectToAction("Index");
                }

                uint deviceID = pointAcces.DeviceID;

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
                    if (result) TempData["Message"] = $"Porte {pointAcces.Nom} verrouillée avec succès";
                    isUnlocked = false;
                }
                else
                {
                    // La porte est verrouillée ou état inconnu, donc déverrouiller
                    result = await _doorService.UnlockAsync(deviceID, new[] { doorID });
                    if (result) TempData["Message"] = $"Porte {pointAcces.Nom} déverrouillée avec succès";
                    isUnlocked = true;
                }

                if (result)
                {
                    // Mettre à jour l'état dans la base de données
                    pointAcces.EstVerrouille = !isUnlocked;
                    pointAcces.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();

                    // Enregistrer l'événement dans l'historique si c'est un déverrouillage
                    if (isUnlocked)
                    {
                        // Pour démonstration, utilisons l'ID d'utilisateur 1 (administrateur par défaut)
                        // Dans un vrai système, vous utiliseriez l'utilisateur connecté
                        int utilisateurId = 1;

                        var utilisateur = await _context.Utilisateurs.FindAsync(utilisateurId);
                        if (utilisateur != null)
                        {
                            var pointage = new Pointage
                            {
                                UtilisateurId = utilisateurId,
                                Date = DateTime.Today,
                                HeureEntree = DateTime.Now,
                                PointAccesId = pointAcces.Id
                            };

                            _context.Pointages.Add(pointage);
                            await _context.SaveChangesAsync();
                        }
                    }
                }
                else
                {
                    TempData["Error"] = $"Erreur lors du changement d'état de la porte {pointAcces.Nom}";
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
                // Récupérer le point d'accès pour connaître son device ID
                var pointAcces = await _context.PointsAcces.FirstOrDefaultAsync(p => p.DoorID == doorID);

                if (pointAcces == null)
                {
                    TempData["Error"] = $"Point d'accès non trouvé pour la porte {doorID}";
                    return RedirectToAction("Index");
                }

                uint deviceID = pointAcces.DeviceID;

                // Supprimer la porte via l'API
                var result = await _doorService.DeleteAsync(deviceID, new[] { doorID });

                if (result)
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

                    TempData["Message"] = $"Porte {pointAcces.Nom} supprimée avec succès";
                }
                else
                {
                    TempData["Error"] = $"Erreur lors de la suppression de la porte {pointAcces.Nom}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la suppression de la porte {doorID}");
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("Index");
        }

        // GET: /Door/DoorHistory/{pointAccesId}
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
                    .OrderByDescending(p => p.HeureEntree)
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

        // GET: /Door/EditDoor/{pointAccesId}
        public async Task<IActionResult> EditDoor(int pointAccesId)
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

                // Récupérer les appareils pour le dropdown
                var devices = await _deviceService.GetAllDevicesAsync();
                ViewBag.Devices = devices;

                // Créer le modèle pour l'édition
                var model = new DoorInfoModel
                {
                    DoorID = pointAcces.DoorID,
                    Name = pointAcces.Nom,
                    DeviceID = pointAcces.DeviceID,
                    RelayPort = pointAcces.RelayPort,
                    Description = pointAcces.Description,
                    PointAccesId = pointAcces.Id
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement du formulaire d'édition");
                TempData["Error"] = "Une erreur est survenue: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // POST: /Door/EditDoor
        [HttpPost]
        public async Task<IActionResult> EditDoor(DoorInfoModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["Error"] = "Données de formulaire invalides";
                    return RedirectToAction("EditDoor", new { pointAccesId = model.PointAccesId });
                }

                // Récupérer le point d'accès existant
                var pointAcces = await _context.PointsAcces.FindAsync(model.PointAccesId);

                if (pointAcces == null)
                {
                    TempData["Error"] = "Point d'accès non trouvé";
                    return RedirectToAction("Index");
                }

                // Mettre à jour les informations dans l'API
                var doorInfo = await GetDoorInfoFromApi(pointAcces.DeviceID, pointAcces.DoorID);

                if (doorInfo != null)
                {
                    // Mettre à jour les propriétés qui peuvent être modifiées
                    doorInfo.Name = model.Name;
                    // Si le device ID a changé, mettre à jour les autres propriétés associées
                    if (doorInfo.EntryDeviceID != model.DeviceID)
                    {
                        doorInfo.EntryDeviceID = model.DeviceID;
                        doorInfo.ExitDeviceID = model.DeviceID;
                        doorInfo.Relay.DeviceID = model.DeviceID;
                        doorInfo.Sensor.DeviceID = model.DeviceID;
                        doorInfo.Button.DeviceID = model.DeviceID;
                    }
                    // Mettre à jour le port du relais
                    doorInfo.Relay.Port = model.RelayPort;

                    // Supprimer l'ancienne porte
                    await _doorService.DeleteAsync(pointAcces.DeviceID, new[] { pointAcces.DoorID });

                    // Ajouter la porte mise à jour
                    var result = await _doorService.AddAsync(model.DeviceID, new[] { doorInfo });

                    if (result)
                    {
                        // Mettre à jour la base de données
                        pointAcces.Nom = model.Name;
                        pointAcces.DeviceID = model.DeviceID;
                        pointAcces.RelayPort = model.RelayPort;
                        pointAcces.Description = model.Description;
                        pointAcces.UpdatedAt = DateTime.Now;

                        await _context.SaveChangesAsync();

                        TempData["Message"] = "Point d'accès mis à jour avec succès";
                    }
                    else
                    {
                        TempData["Error"] = "Erreur lors de la mise à jour du point d'accès dans l'API";
                    }
                }
                else
                {
                    TempData["Error"] = "Impossible de trouver la porte dans l'API";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du point d'accès");
                TempData["Error"] = "Une erreur est survenue: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // GET: /Door/Monitor
        public async Task<IActionResult> Monitor()
        {
            try
            {
                // Récupérer tous les points d'accès
                var pointsAcces = await _context.PointsAcces.ToListAsync();

                // Récupérer les appareils
                var devices = await _deviceService.GetAllDevicesAsync();

                // Préparer le modèle pour la vue
                var doorStatuses = new List<DoorStatusModel>();

                foreach (var pointAcces in pointsAcces)
                {
                    // Récupérer le statut depuis l'API
                    var apiStatuses = await _doorService.GetStatusAsync(pointAcces.DeviceID);
                    var apiStatus = apiStatuses.FirstOrDefault(s => s.DoorID == pointAcces.DoorID);

                    if (apiStatus != null)
                    {
                        doorStatuses.Add(new DoorStatusModel
                        {
                            DoorID = pointAcces.DoorID,
                            IsOpen = apiStatus.IsOpen,
                            IsUnlocked = apiStatus.IsUnlocked,
                            HeldOpen = apiStatus.HeldOpen,
                            AlarmFlags = apiStatus.AlarmFlags
                        });
                    }
                }

                return View(doorStatuses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement de la page de surveillance");
                TempData["Error"] = "Une erreur est survenue: " + ex.Message;
                return View(new List<DoorStatusModel>());
            }
        }

        // Méthode d'assistance pour obtenir le nom d'un appareil à partir de son ID
        private string GetDeviceName(List<DeviceInfoModel> devices, uint deviceID)
        {
            var device = devices.Find(d => d.DeviceID == deviceID);
            return device?.DeviceName ?? $"Appareil {deviceID}";
        }

        // Méthode pour récupérer les informations d'une porte depuis l'API
        private async Task<Door.DoorInfo> GetDoorInfoFromApi(uint deviceID, uint doorID)
        {
            try
            {
                // Récupérer toutes les portes pour cet appareil
                var doorInfos = await _doorService.GetListAsync(deviceID);

                // Trouver la porte spécifique
                var doorInfo = doorInfos.FirstOrDefault(d => d.DoorID == doorID);

                return doorInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la récupération des informations de la porte {doorID} depuis l'API");
                return null;
            }
        }
    }
}