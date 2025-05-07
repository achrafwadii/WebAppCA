using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using WebAppCA.Services;
using WebAppCA.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace WebAppCA.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        private readonly ILogger<HealthController> _logger;
        private readonly ConnectSvc _connectSvc;
        private readonly DoorService _doorService;
        private readonly ApplicationDbContext _context;

        public HealthController(
            ILogger<HealthController> logger,
            ConnectSvc connectSvc,
            DoorService doorService,
            ApplicationDbContext context)
        {
            _logger = logger;
            _connectSvc = connectSvc;
            _doorService = doorService;
            _context = context;
        }

        [HttpGet("check")]
        public IActionResult Check()
        {
            try
            {
                // Vérifier si le service de connexion est disponible
                if (!_connectSvc.IsConnected)
                {
                    _logger.LogWarning("La vérification de santé a échoué : service non connecté");
                    return StatusCode(503, new { status = "error", message = "Service non connecté" });
                }

                // Essayer d'obtenir la liste des appareils pour vérifier la connexion
                var devices = _connectSvc.GetDeviceList();
                if (devices == null)
                {
                    _logger.LogWarning("La vérification de santé a échoué : impossible d'obtenir la liste des appareils");
                    return StatusCode(503, new { status = "error", message = "Impossible d'obtenir la liste des appareils" });
                }

                return Ok(new { status = "healthy", message = "Service opérationnel" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification de santé");
                return StatusCode(500, new { status = "error", message = ex.Message });
            }
        }

        [HttpGet("diagnostics")]
        public async Task<IActionResult> RunDiagnostics()
        {
            var results = new Dictionary<string, object>();
            var isHealthy = true;

            try
            {
                // Vérifier la connexion de base
                results["grpc_connected"] = _connectSvc.IsConnected;
                if (!_connectSvc.IsConnected)
                {
                    isHealthy = false;
                    results["grpc_error"] = "Le service gRPC n'est pas connecté";
                }

                // Vérifier la connexion à la base de données
                try
                {
                    results["db_connected"] = await _context.Database.CanConnectAsync();
                    if (!(bool)results["db_connected"])
                    {
                        isHealthy = false;
                        results["db_error"] = "Impossible de se connecter à la base de données";
                    }
                }
                catch (Exception dbEx)
                {
                    isHealthy = false;
                    results["db_connected"] = false;
                    results["db_error"] = dbEx.Message;
                }

                // Vérifier la liste des appareils
                try
                {
                    var devices = _connectSvc.GetDeviceList();
                    results["devices_found"] = devices?.Count ?? 0;
                    if (devices == null || devices.Count == 0)
                    {
                        results["devices_warning"] = "Aucun appareil trouvé";
                    }
                }
                catch (Exception devEx)
                {
                    isHealthy = false;
                    results["devices_found"] = 0;
                    results["devices_error"] = devEx.Message;
                }

                // Si nous avons au moins un appareil, tester la récupération des portes
                if (results["devices_found"] is long && (long)results["devices_found"] > 0)
                {
                    try
                    {
                        var firstDeviceId = _connectSvc.GetDeviceList()[0].DeviceID;
                        var doors = await _doorService.GetListAsync(firstDeviceId);
                        results["doors_found"] = doors?.Count ?? 0;

                        // Tester les statuts des portes
                        var statuses = await _doorService.GetStatusAsync(firstDeviceId);
                        results["door_statuses_found"] = statuses?.Count ?? 0;
                    }
                    catch (Exception doorEx)
                    {
                        isHealthy = false;
                        results["doors_error"] = doorEx.Message;
                    }
                }

                // Ajouter des statistiques générales
                try
                {
                    results["device_count_db"] = await _context.Devices.CountAsync();
                    results["door_count_db"] = await _context.PointsAcces.CountAsync();
                }
                catch (Exception statEx)
                {
                    results["stats_error"] = statEx.Message;
                }

                return Ok(new
                {
                    status = isHealthy ? "healthy" : "unhealthy",
                    timestamp = DateTime.Now,
                    results = results
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'exécution des diagnostics");
                return StatusCode(500, new
                {
                    status = "error",
                    message = ex.Message,
                    results = results
                });
            }
        }
    }
}