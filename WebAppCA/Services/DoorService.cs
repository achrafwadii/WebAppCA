using Door;
using Grpc.Core;
using Google.Protobuf.Collections;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebAppCA.Models;

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

        // Ajout de gestion des erreurs plus robuste
        private async Task<RepeatedField<T>> SafeGrpcCall<T>(Func<Task<RepeatedField<T>>> grpcMethod)
        {
            try
            {
                var result = await grpcMethod();
                return result ?? new RepeatedField<T>();
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, $"Erreur gRPC: {ex.Status}");
                return new RepeatedField<T>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur inattendue lors de l'appel gRPC");
                return new RepeatedField<T>();
            }
        }

        // Méthode GetListAsync utilisée par le contrôleur
        public async Task<RepeatedField<DoorInfo>> GetListAsync(uint deviceID)
        {
            try
            {
                _logger.LogInformation($"Demande GetListAsync pour l'appareil {deviceID}");

                // Vérifier que le canal est disponible
                if (_connectSvc?.Channel == null)
                {
                    _logger.LogError("Erreur: Canal gRPC non disponible");
                    return new RepeatedField<DoorInfo>();
                }

                var client = new Door.Door.DoorClient(_connectSvc.Channel.CreateCallInvoker());
                var request = new GetListRequest { DeviceID = deviceID };

                _logger.LogInformation($"Envoi de la requête GetList pour l'appareil {deviceID}");
                var response = await client.GetListAsync(request);
                _logger.LogInformation($"Réponse obtenue pour l'appareil {deviceID}");
                _logger.LogInformation($"Nombre de portes : {response?.Doors?.Count ?? 0}");

                return response?.Doors ?? new RepeatedField<DoorInfo>();
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, $"Erreur RPC lors de la récupération des portes pour l'appareil {deviceID}: {ex.Status}");
                return new RepeatedField<DoorInfo>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la récupération des portes pour l'appareil {deviceID}");
                return new RepeatedField<DoorInfo>();
            }
        }

        public async Task<List<DoorInfoModel>> GetAllDoorsAsync(uint deviceID)
        {
            try
            {
                _logger.LogInformation($"Demande GetAllDoorsAsync pour l'appareil {deviceID}");

                // Vérifier que le canal est disponible
                if (_connectSvc?.Channel == null)
                {
                    _logger.LogError("Erreur: Canal gRPC non disponible");
                    return new List<DoorInfoModel>();
                }

                var client = new Door.Door.DoorClient(_connectSvc.Channel.CreateCallInvoker());
                var request = new GetListRequest { DeviceID = deviceID };

                _logger.LogInformation($"Envoi de la requête GetList pour l'appareil {deviceID}");
                var response = await client.GetListAsync(request);
                _logger.LogInformation($"Réponse obtenue pour l'appareil {deviceID}");
                _logger.LogInformation($"Nombre de portes : {response?.Doors?.Count ?? 0}");

                var doors = new List<DoorInfoModel>();
                if (response?.Doors != null)
                {
                    foreach (var door in response.Doors)
                    {
                        // Récupérer le statut pour cette porte
                        var doorStatuses = await GetStatusAsync(deviceID);
                        var status = doorStatuses?.FirstOrDefault(s => s.DoorID == door.DoorID);

                        doors.Add(new DoorInfoModel
                        {
                            DoorID = door.DoorID,
                            Name = door.Name ?? $"Porte {door.DoorID}",
                            DeviceID = deviceID,
                            RelayPort = (byte)(door.Relay?.Port ?? 0),
                            Status = status?.IsUnlocked == true ? "Déverrouillée" : "Verrouillée",
                            LastActivity = DateTime.Now // Ou utilisez une propriété appropriée si disponible
                        });
                    }
                }

                return doors;
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, $"Erreur RPC lors de la récupération des portes pour l'appareil {deviceID}: {ex.Status}");
                return new List<DoorInfoModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la récupération des portes pour l'appareil {deviceID}");
                return new List<DoorInfoModel>();
            }
        }

        public async Task<RepeatedField<Door.Status>> GetStatusAsync(uint deviceID)
        {
            try
            {
                _logger.LogInformation($"Demande GetStatusAsync pour l'appareil {deviceID}");

                // Vérifier que le canal est disponible
                if (_connectSvc?.Channel == null)
                {
                    _logger.LogError("Erreur: Canal gRPC non disponible");
                    return new RepeatedField<Door.Status>();
                }

                var client = new Door.Door.DoorClient(_connectSvc.Channel.CreateCallInvoker());
                var request = new GetStatusRequest { DeviceID = deviceID };

                _logger.LogInformation($"Envoi de la requête GetStatus pour l'appareil {deviceID}");
                var response = await client.GetStatusAsync(request);

                // Vérifier si response ou response.Status est null
                if (response == null || response.Status == null)
                {
                    _logger.LogWarning("Réponse ou statuts null pour l'appareil {DeviceID}", deviceID);
                    return new RepeatedField<Door.Status>();
                }

                _logger.LogInformation($"GetStatusAsync: Reçu {response.Status.Count} statuts pour l'appareil {deviceID}");
                return response.Status;
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, $"Erreur RPC lors de la récupération des statuts des portes pour l'appareil {deviceID}: {ex.Status}");
                return new RepeatedField<Door.Status>();
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
                _logger.LogInformation($"Demande AddAsync pour l'appareil {deviceID} avec {doors?.Count() ?? 0} portes");

                if (doors == null || !doors.Any())
                {
                    _logger.LogWarning("Tentative d'ajout de portes avec une collection vide");
                    return false;
                }

                // Vérifier que le canal est disponible
                if (_connectSvc?.Channel == null)
                {
                    _logger.LogError("Erreur: Canal gRPC non disponible");
                    return false;
                }

                var client = new Door.Door.DoorClient(_connectSvc.Channel.CreateCallInvoker());
                var request = new AddRequest { DeviceID = deviceID };
                request.Doors.AddRange(doors);

                foreach (var door in doors)
                {
                    _logger.LogInformation($"Ajout porte: ID={door.DoorID}, Name={door.Name}, RelayPort={door.Relay.Port}");
                }

                _logger.LogInformation($"Envoi de la requête Add pour l'appareil {deviceID}");
                await client.AddAsync(request);
                _logger.LogInformation("Requête Add réussie");

                return true;
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, $"Erreur RPC lors de l'ajout des portes pour l'appareil {deviceID}: {ex.Status}");
                return false;
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
                _logger.LogInformation($"Demande DeleteAsync pour l'appareil {deviceID} avec {doorIDs?.Count() ?? 0} portes");

                if (doorIDs == null || !doorIDs.Any())
                {
                    _logger.LogWarning("Tentative de suppression de portes avec une collection vide");
                    return false;
                }

                // Vérifier que le canal est disponible
                if (_connectSvc?.Channel == null)
                {
                    _logger.LogError("Erreur: Canal gRPC non disponible");
                    return false;
                }

                var client = new Door.Door.DoorClient(_connectSvc.Channel.CreateCallInvoker());
                var request = new DeleteRequest { DeviceID = deviceID };
                request.DoorIDs.AddRange(doorIDs);

                _logger.LogInformation($"Envoi de la requête Delete pour l'appareil {deviceID}");
                await client.DeleteAsync(request);
                _logger.LogInformation("Requête Delete réussie");

                return true;
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, $"Erreur RPC lors de la suppression des portes pour l'appareil {deviceID}: {ex.Status}");
                return false;
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
                _logger.LogInformation($"Demande DeleteAllAsync pour l'appareil {deviceID}");

                // Vérifier que le canal est disponible
                if (_connectSvc?.Channel == null)
                {
                    _logger.LogError("Erreur: Canal gRPC non disponible");
                    return false;
                }

                var client = new Door.Door.DoorClient(_connectSvc.Channel.CreateCallInvoker());
                var request = new DeleteAllRequest { DeviceID = deviceID };

                _logger.LogInformation($"Envoi de la requête DeleteAll pour l'appareil {deviceID}");
                await client.DeleteAllAsync(request);
                _logger.LogInformation("Requête DeleteAll réussie");

                return true;
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, $"Erreur RPC lors de la suppression de toutes les portes pour l'appareil {deviceID}: {ex.Status}");
                return false;
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
                _logger.LogInformation($"Demande LockAsync pour l'appareil {deviceID} avec {doorIDs?.Count() ?? 0} portes");

                if (doorIDs == null || !doorIDs.Any())
                {
                    _logger.LogWarning("Tentative de verrouillage de portes avec une collection vide");
                    return false;
                }

                // Vérifier que le canal est disponible
                if (_connectSvc?.Channel == null)
                {
                    _logger.LogError("Erreur: Canal gRPC non disponible");
                    return false;
                }

                var client = new Door.Door.DoorClient(_connectSvc.Channel.CreateCallInvoker());
                var request = new LockRequest { DeviceID = deviceID, DoorFlag = (uint)doorFlag };
                request.DoorIDs.AddRange(doorIDs);

                _logger.LogInformation($"Envoi de la requête Lock pour l'appareil {deviceID}");
                await client.LockAsync(request);
                _logger.LogInformation("Requête Lock réussie");

                return true;
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, $"Erreur RPC lors du verrouillage des portes pour l'appareil {deviceID}: {ex.Status}");
                return false;
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
                _logger.LogInformation($"Demande UnlockAsync pour l'appareil {deviceID} avec {doorIDs?.Count() ?? 0} portes");

                if (doorIDs == null || !doorIDs.Any())
                {
                    _logger.LogWarning("Tentative de déverrouillage de portes avec une collection vide");
                    return false;
                }

                // Vérifier que le canal est disponible
                if (_connectSvc?.Channel == null)
                {
                    _logger.LogError("Erreur: Canal gRPC non disponible");
                    return false;
                }

                var client = new Door.Door.DoorClient(_connectSvc.Channel.CreateCallInvoker());
                var request = new UnlockRequest { DeviceID = deviceID, DoorFlag = (uint)doorFlag };
                request.DoorIDs.AddRange(doorIDs);

                _logger.LogInformation($"Envoi de la requête Unlock pour l'appareil {deviceID}");
                await client.UnlockAsync(request);
                _logger.LogInformation("Requête Unlock réussie");

                return true;
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, $"Erreur RPC lors du déverrouillage des portes pour l'appareil {deviceID}: {ex.Status}");
                return false;
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
                _logger.LogInformation($"Demande ReleaseAsync pour l'appareil {deviceID}");

                if (doorIDs == null || !doorIDs.Any())
                {
                    _logger.LogWarning("Tentative de libération de portes avec une collection vide");
                    return false;
                }

                // Vérifier que le canal est disponible
                if (_connectSvc?.Channel == null)
                {
                    _logger.LogError("Erreur: Canal gRPC non disponible");
                    return false;
                }

                var client = new Door.Door.DoorClient(_connectSvc.Channel.CreateCallInvoker());
                var request = new ReleaseRequest { DeviceID = deviceID, DoorFlag = (uint)doorFlag };
                request.DoorIDs.AddRange(doorIDs);

                _logger.LogInformation($"Envoi de la requête Release pour l'appareil {deviceID}");
                await client.ReleaseAsync(request);
                _logger.LogInformation("Requête Release réussie");

                return true;
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, $"Erreur RPC lors de la libération des portes pour l'appareil {deviceID}: {ex.Status}");
                return false;
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
                _logger.LogInformation($"Demande SetAlarmAsync pour l'appareil {deviceID}");

                if (doorIDs == null || !doorIDs.Any())
                {
                    _logger.LogWarning("Tentative de définition d'alarme sur des portes avec une collection vide");
                    return false;
                }

                // Vérifier que le canal est disponible
                if (_connectSvc?.Channel == null)
                {
                    _logger.LogError("Erreur: Canal gRPC non disponible");
                    return false;
                }

                var client = new Door.Door.DoorClient(_connectSvc.Channel.CreateCallInvoker());
                var request = new SetAlarmRequest { DeviceID = deviceID, AlarmFlag = (uint)alarmFlag };
                request.DoorIDs.AddRange(doorIDs);

                _logger.LogInformation($"Envoi de la requête SetAlarm pour l'appareil {deviceID}");
                await client.SetAlarmAsync(request);
                _logger.LogInformation("Requête SetAlarm réussie");

                return true;
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, $"Erreur RPC lors de la définition d'alarme sur les portes pour l'appareil {deviceID}: {ex.Status}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la définition d'alarme sur les portes pour l'appareil {DeviceID}", deviceID);
                return false;
            }
        }

        public static DoorInfo CreateDoorInfo(string name, uint doorID, uint deviceID, uint relayPort)
        {
            // Amélioration avec validation des entrées
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Le nom de la porte ne peut pas être vide", nameof(name));

            if (doorID == 0)
                throw new ArgumentException("L'ID de la porte ne peut pas être 0", nameof(doorID));

            if (deviceID == 0)
                throw new ArgumentException("L'ID du dispositif ne peut pas être 0", nameof(deviceID));

            if (relayPort > 255)
                throw new ArgumentException("Le port du relay doit être entre 0 et 255", nameof(relayPort));

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