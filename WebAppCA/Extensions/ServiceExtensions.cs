using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using WebAppCA.Services;
using static connect.Connect;

namespace WebAppCA.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddGrpcServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Register GatewayClient as a singleton
            services.AddSingleton<GatewayClient>();

            // Register ConnectClient as a transient service that will be created using the GatewayClient
            services.AddTransient<connect.Connect.ConnectClient>(provider =>
            {
                var gatewayClient = provider.GetRequiredService<GatewayClient>();

                if (gatewayClient.IsConnected && gatewayClient.ConnectClient != null)
                {
                    return gatewayClient.ConnectClient;
                }

                throw new InvalidOperationException("Gateway client is not connected. Unable to create ConnectClient");
            });

            // Register ConnectSvc as a scoped service
            services.AddScoped<ConnectSvc>(provider =>
            {
                try
                {
                    var client = provider.GetRequiredService<connect.Connect.ConnectClient>();
                    var logger = provider.GetRequiredService<ILogger<ConnectSvc>>();
                    return new ConnectSvc(client, logger);
                }
                catch (Exception ex)
                {
                    var logger = provider.GetRequiredService<ILogger<ConnectSvc>>();
                    logger.LogError(ex, "Failed to create ConnectSvc");
                    throw;
                }
            });

            // Register the watchdog service
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
                    try
                    {
                        if (!_gatewayClient.IsConnected)
                        {
                            _logger.LogWarning("gRPC connection lost or not established, reconnecting...");
                            var address = _configuration.GetValue<string>("GrpcSettings:Address") ?? "localhost";
                            var port = _configuration.GetValue<int>("GrpcSettings:Port", 4000);
                            var useSSL = _configuration.GetValue<bool>("GrpcSettings:UseSSL", true);
                            var ignoreCertErrors = _configuration.GetValue<bool>("GrpcSettings:IgnoreCertErrors", false);

                            if (useSSL)
                            {
                                _logger.LogInformation("Attempting secure connection to {Address}:{Port}", address, port);
                                var connected = await _gatewayClient.ConnectSecure(address, port, ignoreCertErrors);

                                if (!connected)
                                {
                                    _logger.LogWarning("Secure connection failed, will retry in {Interval} minutes", _interval.TotalMinutes);
                                }
                            }
                            else
                            {
                                _logger.LogInformation("Attempting insecure connection to {Address}:{Port}", address, port);
                                var connected = _gatewayClient.Connect(address, port);

                                if (!connected)
                                {
                                    _logger.LogWarning("Insecure connection failed, will retry in {Interval} minutes", _interval.TotalMinutes);
                                }
                            }
                        }
                        else
                        {
                            _logger.LogDebug("gRPC connection is active");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in gRPC watchdog");
                    }

                    await Task.Delay(_interval, stoppingToken);
                }

                _logger.LogInformation("gRPC watchdog stopped");
            }
        }
    }
}
