using connect;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WebAppCA.Services;
using static connect.Connect;

namespace WebAppCA.Extensions
{
    public static class ServiceExtensions
    {
        /// <summary>
        /// Configure et enregistre tous les services gRPC nécessaires
        /// </summary>
        public static IServiceCollection AddGrpcServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Validation de la configuration
            var grpcSettings = configuration.GetSection("GrpcSettings");
            if (!grpcSettings.Exists())
            {
                throw new InvalidOperationException("La section 'GrpcSettings' est manquante dans la configuration");
            }

            // Récupérer et valider les paramètres
            var address = grpcSettings.GetValue<string>("Address") ?? "localhost";
            var port = grpcSettings.GetValue<int>("Port", 4000);
            var useSSL = grpcSettings.GetValue<bool>("UseSSL", true);
            var caPath = grpcSettings.GetValue<string>("CaCertPath");
            var retryAttempts = grpcSettings.GetValue<int>("RetryAttempts", 3);
            var retryDelay = grpcSettings.GetValue<int>("RetryDelaySeconds", 5);

            // Vérification du certificat si SSL est activé
            if (useSSL && !string.IsNullOrEmpty(caPath) && !File.Exists(caPath))
            {
                throw new FileNotFoundException($"Le certificat CA spécifié est introuvable: {caPath}");
            }

            // Enregistrer GatewayClient comme singleton
            services.AddSingleton<GatewayClient>();

            // Configuration initiale du client gRPC au démarrage
            services.AddSingleton<IGrpcInitializer>(provider =>
                new GrpcInitializer(
                    provider.GetRequiredService<GatewayClient>(),
                    provider.GetRequiredService<ILogger<GrpcInitializer>>(),
                    configuration));

            // Configuration du client ConnectClient
            services.AddTransient<ConnectClient>(provider =>
            {
                var gatewayClient = provider.GetRequiredService<GatewayClient>();
                var logger = provider.GetRequiredService<ILogger<ConnectClient>>();

                // Vérifier si déjà connecté
                if (gatewayClient.IsConnected && gatewayClient.ConnectClient != null)
                {
                    return gatewayClient.ConnectClient;
                }

                // Tentative de connexion automatique si non connecté
                logger.LogWarning("Gateway client non connecté, tentative de connexion automatique");
                bool connected = false;

                try
                {
                    var settings = configuration.GetSection("GrpcSettings");
                    var address = settings.GetValue<string>("Address") ?? "localhost";
                    var port = settings.GetValue<int>("Port", 4000);
                    var useSSL = settings.GetValue<bool>("UseSSL", true);
                    var useCaCert = settings.GetValue<bool>("UseCaCert", false);
                    var caPath = settings.GetValue<string>("CaCertPath");
                    var ignoreCertErrors = settings.GetValue<bool>("IgnoreCertErrors", false);

                    if (useSSL)
                    {
                        connected = gatewayClient.ConnectSecure(address, port, ignoreCertErrors).Result;
                    }
                    else if (useCaCert && !string.IsNullOrEmpty(caPath) && File.Exists(caPath))
                    {
                        connected = gatewayClient.Connect(caPath, address, port);
                    }
                    else
                    {
                        connected = gatewayClient.Connect(address, port);
                    }

                    if (connected && gatewayClient.ConnectClient != null)
                    {
                        logger.LogInformation("Connexion gRPC établie avec succès");
                        return gatewayClient.ConnectClient;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Échec de connexion gRPC automatique");
                }

                // Au lieu de lever une exception, on crée un client avec un canal fictif
                // qui lèvera des exceptions contrôlées si on essaie de l'utiliser
                logger.LogWarning("Utilisation d'un client de secours. Les appels échoueront jusqu'à ce que la connexion soit rétablie.");
                return new ConnectClient(new Grpc.Core.Channel("localhost", 0, Grpc.Core.ChannelCredentials.Insecure));
            });

            // Enregistrer ConnectSvc comme service délimité
            services.AddScoped<ConnectSvc>(provider =>
            {
                try
                {
                    var client = provider.GetRequiredService<ConnectClient>();
                    var logger = provider.GetRequiredService<ILogger<ConnectSvc>>();
                    return new ConnectSvc(client, logger);
                }
                catch (Exception ex)
                {
                    var logger = provider.GetRequiredService<ILogger<ConnectSvc>>();
                    logger.LogError(ex, "Échec de création de ConnectSvc");
                    throw;
                }
            });

            // Enregistrer et configurer le service de surveillance de connexion
            services.AddHostedService<GrpcConnectionWatchdog>(provider =>
                new GrpcConnectionWatchdog(
                    provider.GetRequiredService<GatewayClient>(),
                    configuration,
                    provider.GetRequiredService<ILogger<GrpcConnectionWatchdog>>(),
                    TimeSpan.FromSeconds(configuration.GetValue<int>("GrpcSettings:WatchdogIntervalSeconds", 300))
                ));

            return services;
        }
    }

    /// <summary>
    /// Interface pour l'initialisation gRPC au démarrage de l'application
    /// </summary>
    public interface IGrpcInitializer
    {
        Task InitializeAsync();
    }

    /// <summary>
    /// Implémentation de l'initialisation gRPC
    /// </summary>
    public class GrpcInitializer : IGrpcInitializer
    {
        private readonly GatewayClient _gatewayClient;
        private readonly ILogger<GrpcInitializer> _logger;
        private readonly IConfiguration _configuration;

        public GrpcInitializer(
            GatewayClient gatewayClient,
            ILogger<GrpcInitializer> logger,
            IConfiguration configuration)
        {
            _gatewayClient = gatewayClient;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task InitializeAsync()
        {
            _logger.LogInformation("Initialisation de la connexion gRPC au démarrage");

            var settings = _configuration.GetSection("GrpcSettings");
            var address = settings.GetValue<string>("Address") ?? "localhost";
            var port = settings.GetValue<int>("Port", 4000);
            var useSSL = settings.GetValue<bool>("UseSSL", true);
            var ignoreCertErrors = settings.GetValue<bool>("IgnoreCertErrors", false);
            var retryAttempts = Math.Max(1, settings.GetValue<int>("RetryAttempts", 3));
            var retryDelaySeconds = settings.GetValue<int>("RetryDelaySeconds", 5);

            for (int attempt = 1; attempt <= retryAttempts; attempt++)
            {
                try
                {
                    bool connected = false;

                    if (useSSL)
                    {
                        _logger.LogInformation("Tentative {Attempt}/{MaxAttempts} - Connexion sécurisée à {Address}:{Port}",
                            attempt, retryAttempts, address, port);

                        connected = await _gatewayClient.ConnectSecure(address, port, ignoreCertErrors);
                    }
                    else if (settings.GetValue<bool>("UseCaCert", false) && !string.IsNullOrEmpty(settings.GetValue<string>("CaCertPath")))
                    {
                        _logger.LogInformation("Tentative {Attempt}/{MaxAttempts} - Connexion avec certificat CA à {Address}:{Port}",
                            attempt, retryAttempts, address, port);

                        var caFile = settings.GetValue<string>("CaCertPath");
                        if (File.Exists(caFile))
                        {
                            connected = _gatewayClient.Connect(caFile, address, port);
                        }
                        else
                        {
                            _logger.LogError("Fichier de certificat CA introuvable: {CaFile}", caFile);
                            connected = false;
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Tentative {Attempt}/{MaxAttempts} - Connexion non sécurisée à {Address}:{Port}",
                            attempt, retryAttempts, address, port);

                        connected = _gatewayClient.Connect(address, port);
                    }

                    if (connected)
                    {
                        _logger.LogInformation("Connexion gRPC établie avec succès");
                        return;
                    }

                    _logger.LogWarning("Échec de connexion, nouvelle tentative dans {RetryDelay} secondes", retryDelaySeconds);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la tentative {Attempt} de connexion gRPC", attempt);
                }

                if (attempt < retryAttempts)
                {
                    await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds));
                }
            }

            _logger.LogError("Échec de toutes les tentatives de connexion gRPC");
        }
    }

    /// <summary>
    /// Service d'arrière-plan pour surveiller et maintenir la connexion gRPC
    /// </summary>
    public class GrpcConnectionWatchdog : BackgroundService
    {
        private readonly GatewayClient _gatewayClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly TimeSpan _interval;
        private int _consecutiveFailures = 0;
        private const int MAX_CONSECUTIVE_FAILURES = 5;

        public GrpcConnectionWatchdog(
            GatewayClient gatewayClient,
            IConfiguration configuration,
            ILogger<GrpcConnectionWatchdog> logger,
            TimeSpan interval)
        {
            _gatewayClient = gatewayClient;
            _configuration = configuration;
            _logger = logger;
            _interval = interval;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Service de surveillance gRPC démarré avec intervalle de {IntervalSeconds} secondes", _interval.TotalSeconds);

            // Attendre un court délai pour permettre à l'application de démarrer complètement
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (!_gatewayClient.IsConnected)
                    {
                        _logger.LogWarning("Connexion gRPC perdue ou non établie (échecs consécutifs: {FailureCount}), tentative de reconnexion...",
                            _consecutiveFailures);

                        var address = _configuration.GetValue<string>("GrpcSettings:Address") ?? "localhost";
                        var port = _configuration.GetValue<int>("GrpcSettings:Port", 4000);
                        var useSSL = _configuration.GetValue<bool>("GrpcSettings:UseSSL", true);
                        var ignoreCertErrors = _configuration.GetValue<bool>("GrpcSettings:IgnoreCertErrors", false);

                        bool connected = false;

                        if (useSSL)
                        {
                            _logger.LogInformation("Tentative de connexion sécurisée à {Address}:{Port}", address, port);
                            connected = await _gatewayClient.ConnectSecure(address, port, ignoreCertErrors);
                        }
                        else if (_configuration.GetValue<bool>("GrpcSettings:UseCaCert", false))
                        {
                            var caPath = _configuration.GetValue<string>("GrpcSettings:CaCertPath");
                            if (!string.IsNullOrEmpty(caPath) && File.Exists(caPath))
                            {
                                _logger.LogInformation("Tentative de connexion avec certificat CA à {Address}:{Port}", address, port);
                                connected = _gatewayClient.Connect(caPath, address, port);
                            }
                            else
                            {
                                _logger.LogError("Fichier de certificat CA introuvable ou non spécifié");
                                connected = false;
                            }
                        }
                        else
                        {
                            _logger.LogInformation("Tentative de connexion non sécurisée à {Address}:{Port}", address, port);
                            connected = _gatewayClient.Connect(address, port);
                        }

                        if (connected)
                        {
                            _logger.LogInformation("Connexion gRPC rétablie avec succès");
                            _consecutiveFailures = 0;
                        }
                        else
                        {
                            _consecutiveFailures++;

                            // Augmenter progressivement le délai après des échecs consécutifs
                            var adjustedInterval = _consecutiveFailures <= MAX_CONSECUTIVE_FAILURES
                                ? _interval
                                : TimeSpan.FromMinutes(Math.Min(30, _interval.TotalMinutes * 2));

                            _logger.LogWarning("Échec de reconnexion ({FailureCount}/{MaxFailures}), nouvelle tentative dans {Interval}",
                                _consecutiveFailures, MAX_CONSECUTIVE_FAILURES, adjustedInterval);

                            await Task.Delay(adjustedInterval, stoppingToken);
                            continue;
                        }
                    }
                    else
                    {
                        // Test de connexion périodique pour vérifier si elle est toujours active
                        try
                        {
                            var client = _gatewayClient.ConnectClient;
                            if (client != null)
                            {
                                var request = new GetDeviceListRequest();
                                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                                var callOptions = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(5));

                                var response = await client.GetDeviceListAsync(request, callOptions);
                                _logger.LogDebug("Connexion gRPC active, {DeviceCount} appareils disponibles", response.DeviceInfos.Count);
                                _consecutiveFailures = 0;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Test de connexion gRPC a échoué, marquage comme déconnecté");
                            _consecutiveFailures++;
                            // Forcer la déconnexion pour que la prochaine itération tente de se reconnecter
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur dans le service de surveillance gRPC");
                    _consecutiveFailures++;
                }

                await Task.Delay(_interval, stoppingToken);
            }

            _logger.LogInformation("Service de surveillance gRPC arrêté");
        }
    }
}