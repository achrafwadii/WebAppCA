using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using WebAppCA.Services;
using WebAppCA.Models;
using WebAppCA.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace WebAppCA.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DoorApiController : ControllerBase
    {
        private readonly ILogger<DoorApiController> _logger;
        private readonly DoorService _doorService;
        private readonly ConnectSvc _connectSvc;
        private readonly ApplicationDbContext _context;

        public DoorApiController(
            ILogger<DoorApiController> logger,
            DoorService doorService,
            ConnectSvc connectSvc,
            ApplicationDbContext context)
        {
            _logger = logger;
            _doorService = doorService;
            _connectSvc = connectSvc;
            _context = context;
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetServiceStatus()
        {
            try
            {
                // Vérifier la connectivité gRPC
                bool grpcConnected = _connectSvc.IsConnected;

                // Vérifier la connectivité de la base de données
                bool dbConnected = await _context.Database.CanConnectAsync();

                // Récupérer des statistiques
                int deviceCount = await _context.Devices.CountAsync();
                int doorCount = await _context.PointsAcces.CountAsync();

                return Ok(new
                {
                    grpcConnected,
                    dbConnected,
                    stats = new
                    {
                        devices = deviceCount,
                        doors = doorCount
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification du statut");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("devices")]
        public IActionResult GetDevices()
        {
            try
            {
                if (!_connectSvc.IsConnected)
                {
                    return StatusCode(503, new { error = "Service non connecté" });
                }

                var devices = _connectSvc.GetDeviceList();
                var result = devices.Select(d => new {
                    id = d.DeviceID,
                    ip = d.IPAddr,
                    connectionMode = d.ConnectionMode.ToString(),
                    isConnected = true
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des appareils");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("doors/{deviceId}")]
        public async Task<IActionResult> GetDoors(uint deviceId)
        {
            try
            {
                if (!_connectSvc.IsConnected)
                {
                    return StatusCode(503, new { error = "Service non connecté" });
                }

                var doors = await _doorService.GetListAsync(deviceId);
                var statuses = await _doorService.GetStatusAsync(deviceId);

                var result = doors.Select(d => {
                    var status = statuses.FirstOrDefault(s => s.DoorID == d.DoorID);
                    return new
                    {
                        id = d.DoorID,
                        name = d.Name,
                        deviceId = d.EntryDeviceID,
                        relayPort = d.Relay.Port,
                        isOpen = status?.IsOpen ?? false,
                        isUnlocked = status?.IsUnlocked ?? false
                    };
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des portes");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("db/points-acces")]
        public async Task<IActionResult> GetPointsAcces()
        {
            try
            {
                var pointsAcces = await _context.PointsAcces.ToListAsync();
                return Ok(pointsAcces);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des points d'accès");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}