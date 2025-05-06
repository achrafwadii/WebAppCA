using Door;
using Grpc.Core;
using Google.Protobuf.Collections;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebAppCA.Services
{
    public class DoorService
    {
        private readonly ILogger<DoorService> _logger;
        private readonly ConnectSvc _connectSvc;

        public DoorService(ILogger<DoorService> logger, ConnectSvc connectSvc)
        {
            _logger = logger;
            _connectSvc = connectSvc;
        }

        public async Task<RepeatedField<DoorInfo>> GetListAsync(uint deviceID)
        {
            try
            {
                var client = new Door.Door.DoorClient(_connectSvc.Channel.CreateCallInvoker());
                var request = new GetListRequest { DeviceID = deviceID };
                var response = await client.GetListAsync(request);
                return response.Doors;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des portes pour l'appareil {DeviceID}", deviceID);
                return new RepeatedField<DoorInfo>();
            }
        }

        public async Task<RepeatedField<Door.Status>> GetStatusAsync(uint deviceID)
        {
            try
            {
                var client = new Door.Door.DoorClient(_connectSvc.Channel.CreateCallInvoker());
                var request = new GetStatusRequest { DeviceID = deviceID };
                var response = await client.GetStatusAsync(request);
                return response.Status;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statuts des portes pour l'appareil {DeviceID}", deviceID);
                return new RepeatedField<Door.Status>();
            }
        }

        public async Task<bool> AddAsync(uint deviceID, IEnumerable<DoorInfo> doors)
        {
            try
            {
                var client = new Door.Door.DoorClient(_connectSvc.Channel.CreateCallInvoker());
                var request = new AddRequest { DeviceID = deviceID };
                request.Doors.AddRange(doors);
                await client.AddAsync(request);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'ajout des portes pour l'appareil {DeviceID}", deviceID);
                return false;
            }
        }

        public async Task<bool> DeleteAsync(uint deviceID, IEnumerable<uint> doorIDs)
        {
            try
            {
                var client = new Door.Door.DoorClient(_connectSvc.Channel.CreateCallInvoker());
                var request = new DeleteRequest { DeviceID = deviceID };
                request.DoorIDs.AddRange(doorIDs);
                await client.DeleteAsync(request);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression des portes pour l'appareil {DeviceID}", deviceID);
                return false;
            }
        }

        public async Task<bool> DeleteAllAsync(uint deviceID)
        {
            try
            {
                var client = new Door.Door.DoorClient(_connectSvc.Channel.CreateCallInvoker());
                var request = new DeleteAllRequest { DeviceID = deviceID };
                await client.DeleteAllAsync(request);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de toutes les portes pour l'appareil {DeviceID}", deviceID);
                return false;
            }
        }

        public async Task<bool> LockAsync(uint deviceID, IEnumerable<uint> doorIDs, Door.DoorFlag doorFlag = Door.DoorFlag.Operator)
        {
            try
            {
                var client = new Door.Door.DoorClient(_connectSvc.Channel.CreateCallInvoker());
                var request = new LockRequest { DeviceID = deviceID, DoorFlag = (uint)doorFlag };
                request.DoorIDs.AddRange(doorIDs);
                await client.LockAsync(request);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du verrouillage des portes pour l'appareil {DeviceID}", deviceID);
                return false;
            }
        }

        public async Task<bool> UnlockAsync(uint deviceID, IEnumerable<uint> doorIDs, Door.DoorFlag doorFlag = Door.DoorFlag.Operator)
        {
            try
            {
                var client = new Door.Door.DoorClient(_connectSvc.Channel.CreateCallInvoker());
                var request = new UnlockRequest { DeviceID = deviceID, DoorFlag = (uint)doorFlag };
                request.DoorIDs.AddRange(doorIDs);
                await client.UnlockAsync(request);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du déverrouillage des portes pour l'appareil {DeviceID}", deviceID);
                return false;
            }
        }

        public async Task<bool> ReleaseAsync(uint deviceID, IEnumerable<uint> doorIDs, Door.DoorFlag doorFlag = Door.DoorFlag.Operator)
        {
            try
            {
                var client = new Door.Door.DoorClient(_connectSvc.Channel.CreateCallInvoker());
                var request = new ReleaseRequest { DeviceID = deviceID, DoorFlag = (uint)doorFlag };
                request.DoorIDs.AddRange(doorIDs);
                await client.ReleaseAsync(request);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la libération des portes pour l'appareil {DeviceID}", deviceID);
                return false;
            }
        }

        public async Task<bool> SetAlarmAsync(uint deviceID, IEnumerable<uint> doorIDs, Door.AlarmFlag alarmFlag)
        {
            try
            {
                var client = new Door.Door.DoorClient(_connectSvc.Channel.CreateCallInvoker());
                var request = new SetAlarmRequest { DeviceID = deviceID, AlarmFlag = (uint)alarmFlag };
                request.DoorIDs.AddRange(doorIDs);
                await client.SetAlarmAsync(request);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la définition d'alarme sur les portes pour l'appareil {DeviceID}", deviceID);
                return false;
            }
        }

        public static DoorInfo CreateDoorInfo(string name, uint doorID, uint deviceID, uint relayPort)
        {
            return new DoorInfo
            {
                DoorID = doorID,
                Name = name,
                EntryDeviceID = deviceID,
                ExitDeviceID = deviceID,
                Relay = new Relay { DeviceID = deviceID, Port = relayPort },
                Sensor = new Sensor { DeviceID = deviceID, Port = 0 },
                Button = new ExitButton { DeviceID = deviceID, Port = 0 },
                AutoLockTimeout = 5,
                HeldOpenTimeout = 30
            };
        }
    }
}
