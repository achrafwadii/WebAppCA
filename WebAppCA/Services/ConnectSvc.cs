using Connect;
using Grpc.Core;
using Google.Protobuf.Collections;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace WebAppCA.Services
{
    public class ConnectSvc
    {
        private const int SEARCH_TIMEOUT_MS = 5000;
        private readonly ILogger<ConnectSvc> _logger;
        private Connect.Connect.ConnectClient _connectClient;
        private bool _isConnected = false;
        private DateTime _lastConnectionAttempt = DateTime.MinValue;
        private readonly TimeSpan _reconnectCooldown = TimeSpan.FromSeconds(5);

        // Constructor for Grpc.Core.Channel (legacy)
        public ConnectSvc(Channel channel, ILogger<ConnectSvc> logger = null)
        {
            _logger = logger;
            InitializeChannel(channel);
        }

        // Constructor for Grpc.Net.Client.GrpcChannel (newer)
        public ConnectSvc(GrpcChannel channel, ILogger<ConnectSvc> logger = null)
        {
            _logger = logger;
            InitializeChannel(channel);
        }

        public ConnectSvc(ILogger<ConnectSvc> logger = null)
        {
            _logger = logger;
            _logger?.LogWarning("ConnectSvc initialisé sans channel (aucune connexion disponible)");
            _isConnected = false;
        }

        private void InitializeChannel(ChannelBase channel)
        {
            if (channel != null)
            {
                try
                {
                    // Initialiser à la fois le client et la propriété Channel
                    Channel = channel;
                    _connectClient = new Connect.Connect.ConnectClient(channel);
                    // Tester la connexion en récupérant la liste des appareils
                    try
                    {
                        var request = new GetDeviceListRequest { };
                        var response = _connectClient.GetDeviceList(request);
                        _isConnected = true;
                        _logger?.LogInformation("ConnectSvc initialisé avec succès. {Count} appareils trouvés.",
                            response?.DeviceInfos?.Count ?? 0);
                    }
                    catch (RpcException rpcEx)
                    {
                        _logger?.LogError(rpcEx, "Échec du test de connexion initial: {StatusCode} - {Message}",
                            rpcEx.StatusCode, rpcEx.Message);
                        _isConnected = false;
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Erreur lors de l'initialisation du client ConnectSvc: {Message}", ex.Message);
                    _isConnected = false;
                }
            }
            else
            {
                _logger?.LogWarning("ConnectSvc initialisé avec un channel null");
                _isConnected = false;
            }
        }

        public bool IsConnected => _isConnected && _connectClient != null;

        // La propriété Channel est utilisée par d'autres services
        public ChannelBase Channel { get; private set; }

        public async Task<bool> TryReconnectAsync()
        {
            if (IsConnected) return true;

            // Éviter les tentatives trop fréquentes de reconnexion
            if (DateTime.Now - _lastConnectionAttempt < _reconnectCooldown)
            {
                return false;
            }

            _lastConnectionAttempt = DateTime.Now;

            try
            {
                if (Channel != null)
                {
                    _connectClient = new Connect.Connect.ConnectClient(Channel);
                    var request = new GetDeviceListRequest { };
                    var response = await _connectClient.GetDeviceListAsync(request);
                    _isConnected = true;
                    _logger?.LogInformation("Reconnexion réussie. {Count} appareils trouvés.",
                        response?.DeviceInfos?.Count ?? 0);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Échec de la tentative de reconnexion: {Message}", ex.Message);
                _isConnected = false;
                return false;
            }
        }

        private bool EnsureConnectClient()
        {
            if (_connectClient == null || !_isConnected)
            {
                var errorMessage = "Le service de connexion gRPC n'est pas disponible. Vérifiez que le serveur gRPC est en cours d'exécution.";
                _logger?.LogError(errorMessage);
                return false;
            }
            return true;
        }

        public RepeatedField<Connect.DeviceInfo> GetDeviceList()
        {
            if (!EnsureConnectClient())
                return new RepeatedField<Connect.DeviceInfo>();

            try
            {
                var request = new GetDeviceListRequest { };
                var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(5));
                var response = _connectClient.GetDeviceList(request, options);
                return response.DeviceInfos;
            }
            catch (RpcException ex)
            {
                _logger?.LogError(ex, "Erreur lors de l'appel à GetDeviceList: {StatusCode} - {Message}", ex.StatusCode, ex.Message);
                _isConnected = false;
                return new RepeatedField<Connect.DeviceInfo>();
            }
        }


        public uint Connect(Connect.ConnectInfo connectInfo)
        {
            if (!EnsureConnectClient())
                return 0;

            try
            {
                _logger?.LogInformation("Tentative de connexion à {IPAddr}:{Port} avec UseSSL={UseSSL}",
                    connectInfo.IPAddr, connectInfo.Port, connectInfo.UseSSL);

                var request = new ConnectRequest { ConnectInfo = connectInfo };

                // Ajout d'un timeout pour la requête RPC
                var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(10));
                var response = _connectClient.Connect(request, options);

                _logger?.LogInformation("Connexion réussie. DeviceID: {DeviceID}", response.DeviceID);

                if (response.DeviceID <= 0)
                {
                    _logger?.LogWarning("Le service a retourné un DeviceID invalide: {DeviceID}", response.DeviceID);
                }

                return response.DeviceID;
            }
            catch (RpcException ex)
            {
                _logger?.LogError(ex, "Erreur lors de l'appel à Connect pour {IPAddr}:{Port}: {StatusCode} - {Message}",
                    connectInfo.IPAddr, connectInfo.Port, ex.StatusCode, ex.Message);

                // Log detailed status
                _logger?.LogError("Status détaillé: {Status}", ex.Status.ToString() ?? "Inconnu");

                // En cas d'erreur de délai d'attente, essayons à nouveau avec un délai plus long
                if (ex.StatusCode == Grpc.Core.StatusCode.DeadlineExceeded)
                {
                    _logger?.LogWarning("Délai d'attente dépassé. Nouvelle tentative avec un délai plus long...");
                    try
                    {
                        var request = new ConnectRequest { ConnectInfo = connectInfo };
                        var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(20));
                        var response = _connectClient.Connect(request, options);
                        return response.DeviceID;
                    }
                    catch (Exception retryEx)
                    {
                        _logger?.LogError(retryEx, "La seconde tentative a également échoué");
                    }
                }

                _isConnected = false;
                return 0;
            }
        }

        // Les autres méthodes restent les mêmes...
        public RepeatedField<Connect.SearchDeviceInfo> SearchDevice()
        {
            if (!EnsureConnectClient())
                return new RepeatedField<Connect.SearchDeviceInfo>();

            try
            {
                var request = new SearchDeviceRequest { Timeout = SEARCH_TIMEOUT_MS };
                var options = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(SEARCH_TIMEOUT_MS / 1000 + 2));
                var response = _connectClient.SearchDevice(request, options);
                return response.DeviceInfos;
            }
            catch (RpcException ex)
            {
                _logger?.LogError(ex, "Erreur lors de l'appel à SearchDevice: {StatusCode} - {Message}", ex.StatusCode, ex.Message);
                _isConnected = false;
                return new RepeatedField<Connect.SearchDeviceInfo>();
            }
        }

        public void Disconnect(uint[] deviceIDs)
        {
            if (!EnsureConnectClient())
                return;

            try
            {
                var request = new DisconnectRequest { };
                request.DeviceIDs.AddRange(deviceIDs);
                _connectClient.Disconnect(request);
            }
            catch (RpcException ex)
            {
                _logger?.LogError(ex, "Erreur lors de l'appel à Disconnect: {StatusCode} - {Message}", ex.StatusCode, ex.Message);
                _isConnected = false;
            }
        }

        public void DisconnectAll()
        {
            if (!EnsureConnectClient())
                return;

            try
            {
                var request = new DisconnectAllRequest { };
                _connectClient.DisconnectAll(request);
            }
            catch (RpcException ex)
            {
                _logger?.LogError(ex, "Erreur lors de l'appel à DisconnectAll: {StatusCode} - {Message}", ex.StatusCode, ex.Message);
                _isConnected = false;
            }
        }
    }
}