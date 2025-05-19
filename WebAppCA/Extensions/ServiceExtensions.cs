using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using WebAppCA.Services;
using static Grpcconnect.Connect;

namespace WebAppCA.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddGrpcServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<GatewayClient>();
            services.AddScoped<ConnectSvc>(provider =>
            {
                var client = provider.GetRequiredService<ConnectClient>();
                var logger = provider.GetRequiredService<ILogger<ConnectSvc>>();

                return new ConnectSvc(client, logger);
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
                        _logger.LogWarning("gRPC lost, reconnecting...");
                        var address = _configuration.GetValue<string>("GrpcSettings:Address") ?? "localhost";
                        var port = _configuration.GetValue<int>("GrpcSettings:Port", 4000);
                        var useSSL = _configuration.GetValue<bool>("GrpcSettings:UseSSL", true);

                        if (useSSL)
                        {
                            await _gatewayClient.ConnectSecure(address, port, true);
                        }
                        else
                        {
                            _gatewayClient.Connect(address, port);
                        }
                    }

                    await Task.Delay(_interval, stoppingToken);
                }

                _logger.LogInformation("gRPC watchdog stopped");
            }
        }
    }
}
