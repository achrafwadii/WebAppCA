using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using WebAppCA.Services;

namespace WebAppCA.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddGrpcServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<GatewayClient>();
            services.AddScoped<ConnectSvc>(provider =>
            {
                var gateway = provider.GetRequiredService<GatewayClient>();
                var logger = provider.GetRequiredService<ILogger<ConnectSvc>>();
                return new ConnectSvc(gateway.Channel, logger);
            });

            services.AddHostedService<GrpcConnectionWatchdog>();
            return services;
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
                        await _gatewayClient.Connect(address, port);
                    }

                    await Task.Delay(_interval, stoppingToken);
                }

                _logger.LogInformation("gRPC watchdog stopped");
            }
        }
    }
}
