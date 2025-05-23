﻿using Microsoft.AspNetCore.Mvc;
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

        public async Task<IActionResult> Index(uint? selectedDeviceId = null)
        {
            try
            {
                // Ajouter des logs de débogage
                _logger.LogInformation("Début de la méthode Index");

                // Vérifiez si les appareils sont bien récupérés
                var devices = await _deviceService.GetAllDevicesAsync();
                _logger.LogInformation($"Nombre d'appareils récupérés : {devices?.Count ?? 0}");

                if (devices == null || !devices.Any())
                {
                    _logger.LogWarning("Aucun appareil trouvé");
                    ViewBag.Devices = new List<DeviceInfoModel>();
                    return View(new List<DoorInfoModel>());
                }

                // Sélectionner le premier appareil si aucun n'est spécifié
                var deviceID = selectedDeviceId ?? (uint)devices[0].DeviceID;
                _logger.LogInformation($"ID de l'appareil sélectionné : {deviceID}");

                // Récupérer les portes pour cet appareil
                var doorInfos = await _doorService.GetListAsync(deviceID);
                _logger.LogInformation($"Nombre de portes récupérées : {doorInfos?.Count ?? 0}");

                // Convertir les portes en DoorInfoModel
                var doors = new List<DoorInfoModel>();
                if (doorInfos != null)
                {
                    foreach (var doorInfo in doorInfos)
                    {
                        var doorStatus = await _doorService.GetStatusAsync(deviceID);
                        var status = doorStatus?.FirstOrDefault(s => s.DoorID == doorInfo.DoorID);

                        doors.Add(new DoorInfoModel
                        {
                            DoorID = doorInfo.DoorID,
                            Name = doorInfo.Name,
                            DeviceID = doorInfo.EntryDeviceID,
                            RelayPort = (byte)doorInfo.Relay.Port,
                            DeviceName = devices.FirstOrDefault(d => d.DeviceID == doorInfo.EntryDeviceID)?.DeviceName,
                            Status = status?.IsUnlocked == true ? "Déverrouillée" : "Verrouillée"
                        });
                    }
                }

                ViewBag.Devices = devices;
                return View(doors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement de la page Index");
                ViewBag.Devices = new List<DeviceInfoModel>();
                return View(new List<DoorInfoModel>());
            }
        }
        // POST: /Door/FilterByDevice
        [HttpPost]
        public IActionResult FilterByDevice(uint deviceId)
        {
            return RedirectToAction("Index", new { selectedDeviceId = deviceId });
        }
        

        // Ajouter des méthodes de validation personnalisées
        private bool ValidateDoorModel(AddDoorModel model)
        {
            if (model.DeviceID <= 0)
                return false;

            if (string.IsNullOrWhiteSpace(model.DoorName))
                return false;

            // Ajouter d'autres validations spécifiques
            return true;
        }

        // POST: /Door/AddDoor
        [HttpPost]
        public async Task<IActionResult> AddDoor(AddDoorModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model); // Retourner à la vue avec les erreurs de validation
            }

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
                ModelState.AddModelError(string.Empty, "Une erreur est survenue lors de l'ajout de la porte.");
                return View(model);
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