// WebAppCA/Extensions/ServiceExtensions.cs
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using WebAppCA.Services;

namespace WebAppCA.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddGrpcServices(this IServiceCollection services, IConfiguration configuration)
        {
            // GatewayClient en singleton
            services.AddSingleton<GatewayClient>();
            services.AddHostedService<GrpcConnectionWatchdog>();

            // ConnectSvc scoped, on force le canal dev-mode
            services.AddScoped<ConnectSvc>(provider =>
            {
                var gateway = provider.GetRequiredService<GatewayClient>();
                var logger = provider.GetRequiredService<ILogger<ConnectSvc>>();
                var address = configuration.GetValue<string>("GrpcSettings:Address") ?? "localhost";
                var port = configuration.GetValue<int>("GrpcSettings:Port", 51211);

                if (!gateway.IsConnected)
                {
                    logger.LogInformation("Initialising gRPC in dev mode at {Address}:{Port}", address, port);
                    gateway.Connect(address, port);
                }

                // passe directement _coreChannel au service
                return new ConnectSvc(gateway.Channel, logger);
            });

            return services;
        }
    }

    class GrpcConnectionWatchdog : BackgroundService
    {
        readonly GatewayClient _gatewayClient;
        readonly IConfiguration _configuration;
        readonly ILogger _logger;
        readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

        public GrpcConnectionWatchdog(
            GatewayClient gatewayClient,
            IConfiguration configuration,
            ILogger<GrpcConnectionWatchdog> logger)
        {
            _gatewayClient = gatewayClient;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("gRPC watchdog started");

            while (!stoppingToken.IsCancellationRequested)
            {
                if (!_gatewayClient.IsConnected)
                {
                    _logger.LogWarning("gRPC lost, reconnecting in dev mode");
                    var address = _configuration.GetValue<string>("GrpcSettings:Address") ?? "localhost";
                    var port = _configuration.GetValue<int>("GrpcSettings:Port", 51211);
                    _gatewayClient.Connect(address, port);
                }

                await Task.Delay(_interval, stoppingToken);
            }

            _logger.LogInformation("gRPC watchdog stopped");
        }
    }
}
