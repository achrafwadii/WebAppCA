// WebAppCA/Services/ConnectSvc.cs - Version améliorée
using System;
using System.Threading.Tasks;
using Grpc.Core;
using Google.Protobuf.Collections;
using Microsoft.Extensions.Logging;
using connect;
using static connect.Connect;
using Grpc.Net.Client;
using System.Net.Http;

namespace WebAppCA.Services
{
    public class ConnectSvc
    {
        private const string GATEWAY_CA_FILE = "Certs/ca.crt";
        private const string GATEWAY_ADDR = "192.168.0.2";
        private const int GATEWAY_PORT = 4000;
        private const int SEARCH_TIMEOUT_MS = 5000;
        private readonly ILogger<ConnectSvc> _logger;
        private readonly ConnectClient _client;
        private bool _isConnected;
        private bool _serviceImplemented = true; // Nouvel indicateur pour suivre si le service est implémenté
        private DateTime _lastAttempt;
        private static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan LongCooldown = TimeSpan.FromMinutes(5); // Temps d'attente plus long après erreur service non implémenté

        public bool IsConnected => _isConnected && _serviceImplemented;
        public bool IsServiceImplemented => _serviceImplemented;
        public ChannelBase Channel { get; }

        public ConnectSvc(ConnectClient client, ILogger<ConnectSvc> logger = null)
        {
            _logger = logger;
            _client = client;
            TestInitialConnection();
        }

        private void TestInitialConnection()
        {
            try
            {
                var resp = _client.GetDeviceList(new GetDeviceListRequest());
                _isConnected = true;
                _serviceImplemented = true;
                _logger?.LogInformation("Init ok. Devices: {0}", resp.DeviceInfos.Count);
            }
            catch (RpcException ex)
            {
                _isConnected = false;

                // Détection spécifique de l'erreur "service non implémenté"
                if (ex.StatusCode == StatusCode.Unimplemented &&
                    ex.Status.Detail.Contains("unknown service connect.Connect"))
                {
                    _serviceImplemented = false;
                    _logger?.LogError("Service gRPC non implémenté sur le serveur. Ce service n'est pas disponible: {Detail}", ex.Status.Detail);
                }
                else
                {
                    _logger?.LogError(ex, "Init failed: {0}", ex.Status);
                }
            }
        }

        public async Task<bool> TryReconnectAsync()
        {
            // Si le service n'est pas implémenté, on impose un délai plus long pour éviter les tentatives inutiles
            TimeSpan currentCooldown = _serviceImplemented ? Cooldown : LongCooldown;

            if (_isConnected) return true;
            if (DateTime.Now - _lastAttempt < currentCooldown) return false;

            // Si le service n'est pas implémenté, on vérifie moins fréquemment
            if (!_serviceImplemented)
            {
                _logger?.LogWarning("Le service Connect est marqué comme non implémenté. Tentative peu fréquente de vérification.");
            }

            _lastAttempt = DateTime.Now;

            try
            {
                var resp = await _client.GetDeviceListAsync(new GetDeviceListRequest());
                _isConnected = true;
                _serviceImplemented = true;
                _logger?.LogInformation("Reconnected. Devices: {0}", resp.DeviceInfos.Count);
                return true;
            }
            catch (RpcException ex)
            {
                _isConnected = false;

                // Détection spécifique de l'erreur "service non implémenté"
                if (ex.StatusCode == StatusCode.Unimplemented &&
                    ex.Status.Detail.Contains("unknown service connect.Connect"))
                {
                    _serviceImplemented = false;
                    _logger?.LogError("Service gRPC non implémenté sur le serveur. Ce service n'est pas disponible: {Detail}", ex.Status.Detail);
                }
                else
                {
                    _logger?.LogError(ex, "Reconnect failed: {0}", ex.Status);
                }

                return false;
            }
            catch (Exception ex)
            {
                _isConnected = false;
                _logger?.LogError(ex, "Reconnect failed: {0}", ex.Message);
                return false;
            }
        }

        public void Initialize(Channel channel)
        {
            try
            {
                if (channel == null)
                {
                    _logger?.LogError("Channel is null during initialization");
                    throw new ArgumentNullException(nameof(channel));
                }

                _logger?.LogInformation("ConnectClient initialized successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to initialize ConnectClient");
                throw;
            }
        }

        private bool EnsureConnected()
        {
            if (!_isConnected)
            {
                _logger?.LogError("gRPC not connected");
                return false;
            }

            if (!_serviceImplemented)
            {
                _logger?.LogError("Service gRPC non implémenté sur le serveur");
                return false;
            }

            return true;
        }

        public RepeatedField<DeviceInfo> GetDeviceList()
        {
            // Si le service n'est pas implémenté, renvoyer directement une liste vide
            if (!_serviceImplemented)
            {
                _logger?.LogWarning("GetDeviceList: Service non disponible sur le serveur");
                return new RepeatedField<DeviceInfo>();
            }

            try
            {
                var request = new GetDeviceListRequest();
                var response = _client.GetDeviceList(request);
                return response.DeviceInfos;
            }
            catch (RpcException ex)
            {
                _isConnected = false;

                // Détection spécifique de l'erreur "service non implémenté"
                if (ex.StatusCode == StatusCode.Unimplemented &&
                    ex.Status.Detail.Contains("unknown service connect.Connect"))
                {
                    _serviceImplemented = false;
                    _logger?.LogError("Service gRPC non implémenté sur le serveur");
                }
                else
                {
                    _logger?.LogError(ex, "GetDeviceList error: {0}", ex.Status);
                }

                return new RepeatedField<DeviceInfo>();
            }
        }

        public uint Connect(ConnectInfo connectInfo)
        {
            // Si le service n'est pas implémenté, renvoyer directement 0
            if (!_serviceImplemented)
            {
                _logger?.LogWarning("Connect: Service non disponible sur le serveur");
                return 0;
            }

            try
            {
                var request = new ConnectRequest { ConnectInfo = connectInfo };
                var response = _client.Connect(request);
                _logger?.LogInformation("Successfully connected to device with ID: {DeviceID}", response.DeviceID);
                return response.DeviceID;
            }
            catch (RpcException ex)
            {
                _isConnected = false;

                // Détection spécifique de l'erreur "service non implémenté"
                if (ex.StatusCode == StatusCode.Unimplemented &&
                    ex.Status.Detail.Contains("unknown service connect.Connect"))
                {
                    _serviceImplemented = false;
                    _logger?.LogError("Service Connect non implémenté sur le serveur");
                }
                else
                {
                    _logger?.LogError(ex, "Connect error: {Status}", ex.Status);
                }

                return 0;
            }
            catch (Exception ex)
            {
                _isConnected = false;
                _logger?.LogError(ex, "Unexpected error in Connect: {Message}", ex.Message);
                return 0;
            }
        }

        public RepeatedField<SearchDeviceInfo> SearchDevice()
        {
            if (!_serviceImplemented)
            {
                _logger?.LogWarning("SearchDevice: Service non disponible sur le serveur");
                return new RepeatedField<SearchDeviceInfo>();
            }

            try
            {
                var request = new SearchDeviceRequest { Timeout = SEARCH_TIMEOUT_MS };
                var response = _client.SearchDevice(request);
                return response.DeviceInfos;
            }
            catch (RpcException ex)
            {
                _isConnected = false;

                if (ex.StatusCode == StatusCode.Unimplemented &&
                    ex.Status.Detail.Contains("unknown service connect.Connect"))
                {
                    _serviceImplemented = false;
                    _logger?.LogError("Service non implémenté sur le serveur");
                }
                else
                {
                    _logger?.LogError(ex, "SearchDevice error: {0}", ex.Status);
                }

                return new RepeatedField<SearchDeviceInfo>();
            }
        }

        // Les autres méthodes du service (Disconnect, DisconnectAll, etc.) suivent le même modèle
        // Vérification préalable de _serviceImplemented
        // Gestion spécifique de l'erreur de service non implémenté

        // Méthode utilitaire pour les contrôleurs
        public bool IsServiceAvailable()
        {
            return _isConnected && _serviceImplemented;
        }

        // Méthode pour la mise en mode dégradé
        public bool EnableDegradedMode()
        {
            _logger?.LogWarning("Activation du mode dégradé pour le service ConnectSvc");
            _serviceImplemented = false;
            return true;
        }

        // Méthode pour réinitialiser et forcer une tentative de reconnexion
        public async Task<bool> ForceReconnectAsync()
        {
            _isConnected = false;
            _serviceImplemented = true; // On suppose que le service pourrait être disponible
            _lastAttempt = DateTime.MinValue; // Réinitialiser le temps d'attente
            return await TryReconnectAsync();
        }
    }
}