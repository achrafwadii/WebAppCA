using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebAppCA.Data;
using WebAppCA.Models;
using Microsoft.EntityFrameworkCore;

namespace WebAppCA.Services
{
    public class DeviceService
    {
        private readonly ILogger<DeviceService> _logger;
        private readonly ApplicationDbContext _context;

        public DeviceService(ILogger<DeviceService> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // Récupérer tous les appareils
        public async Task<List<DeviceInfoModel>> GetAllDevicesAsync()
        {
            try
            {
                var devices = await _context.Devices
                    .Select(d => new DeviceInfoModel
                    {
                        DeviceID = d.DeviceID,
                        DeviceName = d.DeviceName ?? $"Appareil {d.DeviceID}",
                        IPAddress = d.IPAddress,
                        Port = d.Port,
                        Status = d.Status ?? (d.IsConnected == true ? "Connecté" : "Déconnecté"),
                        LastConnection = d.LastConnectionTime
                    })
                    .ToListAsync();

                return devices;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des appareils");
                return new List<DeviceInfoModel>();
            }
        }

        // Récupérer un appareil par ID
        public async Task<DeviceInfoModel> GetDeviceByIdAsync(int deviceId)
        {
            try
            {
                var device = await _context.Devices
                    .Where(d => d.DeviceId == deviceId)
                    .Select(d => new DeviceInfoModel
                    {
                        DeviceID = d.DeviceID,
                        DeviceName = d.DeviceName ?? $"Appareil {d.DeviceID}",
                        IPAddress = d.IPAddress,
                        Port = d.Port,
                        Status = d.Status ?? (d.IsConnected == true ? "Connecté" : "Déconnecté"),
                        LastConnection = d.LastConnectionTime
                    })
                    .FirstOrDefaultAsync();

                return device;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de l'appareil {DeviceId}", deviceId);
                return null;
            }
        }

        // Ajouter un appareil fictif pour tester (à enlever en production)
        public async Task<bool> AddTestDevicesIfEmptyAsync()
        {
            try
            {
                if (!await _context.Devices.AnyAsync())
                {
                    _context.Devices.AddRange(
                        new DeviceInfo
                        {
                            DeviceID = 1,
                            DeviceName = "Contrôleur d'accès - Entrée principale",
                            IPAddress = "192.168.1.100",
                            Port = 51211,
                            UseSSL = true,
                            Description = "Contrôleur entrée principale",
                            LastConnectionTime = DateTime.Now,
                            IsConnected = true,
                            Status = "Connecté"
                        },
                        new DeviceInfo
                        {
                            DeviceID = 2,
                            DeviceName = "Contrôleur d'accès - Salle Serveurs",
                            IPAddress = "192.168.1.101",
                            Port = 51211,
                            UseSSL = true,
                            Description = "Contrôleur salle serveurs",
                            LastConnectionTime = DateTime.Now.AddDays(-1),
                            IsConnected = false,
                            Status = "Déconnecté"
                        }
                    );

                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'ajout des appareils de test");
                return false;
            }
        }
    }
}