using Door;
using Grpc.Core;
using Google.Protobuf.Collections;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebAppCA.Models;
using WebAppCA.Data;
using Microsoft.EntityFrameworkCore;

namespace WebAppCA.Services
{
    public class DoorService
    {
        private readonly ILogger<DoorService> _logger;
        private readonly ConnectSvc _connectSvc;
        private readonly ApplicationDbContext _context;

        public DoorService(ILogger<DoorService> logger, ConnectSvc connectSvc, ApplicationDbContext context)
        {
            _logger = logger;
            _connectSvc = connectSvc;
            _context = context;
        }

        // Récupérer la liste des portes d'un appareil
        public async Task<List<DoorInfoModel>> GetDoorsAsync(int deviceID)
        {
            try
            {
                // Create the client using the channel's CreateCallInvoker method
                var client = new Door.Door.DoorClient(_connectSvc.Channel.CreateCallInvoker());
                var request = new GetListRequest { DeviceID = (uint)deviceID };
                var response = await client.GetListAsync(request);

                var doors = response.Doors.Select(d => new DoorInfoModel
                {
                    DoorID = d.DoorID,
                    Name = d.Name,
                    DeviceID = d.EntryDeviceID,
                    RelayPort = (byte)d.Relay.Port,
                    DeviceName = GetDeviceNameById(d.EntryDeviceID),
                    Status = "Inconnu" // Par défaut, on mettra à jour avec GetDoorStatusAsync
                }).ToList();

                // Mise à jour des statuts
                await UpdateDoorStatusesAsync(doors, deviceID);

                return doors;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des portes pour l'appareil {DeviceID}", deviceID);
                return new List<DoorInfoModel>();
            }
        }

        // Mise à jour des statuts des portes
        private async Task UpdateDoorStatusesAsync(List<DoorInfoModel> doors, int deviceID)
        {
            try
            {
                var client = new Door.Door.DoorClient(_connectSvc.Channel.CreateCallInvoker());
                var request = new GetStatusRequest { DeviceID = (uint)deviceID };
                var response = await client.GetStatusAsync(request);

                foreach (var door in doors)
                {
                    var status = response.Status.FirstOrDefault(s => s.DoorID == door.DoorID);
                    if (status != null)
                    {
                        door.Status = status.IsUnlocked ? "Déverrouillée" : "Verrouillée";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statuts des portes");
            }
        }

        // Ajouter une nouvelle porte
        public async Task<bool> AddDoorAsync(AddDoorModel model)
        {
            try
            {
                var client = new Door.Door.DoorClient(_connectSvc.Channel.CreateCallInvoker());

                var doorInfo = new DoorInfo
                {
                    DoorID = (uint)DateTime.Now.Ticks,
                    Name = model.DoorName,
                    EntryDeviceID = model.DeviceID,
                    ExitDeviceID = model.DeviceID,
                    Relay = new Relay { DeviceID = model.DeviceID, Port = (uint)model.PortNumber },
                    Sensor = new Sensor { DeviceID = model.DeviceID, Port = 0 },
                    Button = new ExitButton { DeviceID = model.DeviceID, Port = 0 },
                    AutoLockTimeout = 5,
                    HeldOpenTimeout = 30
                };

                var request = new AddRequest
                {
                    DeviceID = model.DeviceID
                };
                request.Doors.Add(doorInfo);

                await client.AddAsync(request);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'ajout de la porte");
                return false;
            }
        }

        // Supprimer une porte
        public async Task<bool> DeleteDoorAsync(uint deviceID, uint doorID)
        {
            try
            {
                var client = new Door.Door.DoorClient(_connectSvc.Channel.CreateCallInvoker());
                var request = new DeleteRequest
                {
                    DeviceID = deviceID
                };
                request.DoorIDs.Add(doorID);

                await client.DeleteAsync(request);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de la porte {DoorID}", doorID);
                return false;
            }
        }

        // Verrouiller/Déverrouiller une porte
        public async Task<bool> ToggleDoorAsync(uint deviceID, uint doorID, bool unlock)
        {
            try
            {
                var client = new Door.Door.DoorClient(_connectSvc.Channel.CreateCallInvoker());

                if (unlock)
                {
                    var request = new UnlockRequest
                    {
                        DeviceID = deviceID,
                        DoorFlag = (uint)DoorFlag.Operator
                    };
                    request.DoorIDs.Add(doorID);
                    await client.UnlockAsync(request);
                }
                else
                {
                    var request = new LockRequest
                    {
                        DeviceID = deviceID,
                        DoorFlag = (uint)DoorFlag.Operator
                    };
                    request.DoorIDs.Add(doorID);
                    await client.LockAsync(request);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du changement d'état de la porte {DoorID}", doorID);
                return false;
            }
        }

        // Récupérer le nom d'un appareil par son ID
        private string GetDeviceNameById(uint deviceID)
        {
            var device = _context.Devices.FirstOrDefault(d => d.DeviceID == deviceID);
            return device?.Name ?? $"Appareil {deviceID}";
        }
    }
}