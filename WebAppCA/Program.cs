using Grpc.Core;
using connect;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyApp.Services;
using System;
using System.Threading.Tasks;
using WebAppCA.Data;
using WebAppCA.Extensions;
using WebAppCA.Repositories;
using Grpc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using WebAppCA.Services;
using static connect.Connect;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    // HTTPS + HTTP/2
    options.ListenLocalhost(7211, listenOptions =>
    {
        listenOptions.UseHttps();
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2;
    });
});

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// Permettre HTTP/2 sans TLS
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

builder.Services.AddGrpcClient<ConnectClient>(options =>
{
    // Utilisez HTTPS au lieu de HTTP
    options.Address = new Uri("https://localhost:4000");
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    var handler = new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = 
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    };
    return handler;
});
builder.Services.AddGrpcClient<ConnectClient>(options =>
{
    options.Address = new Uri("http://localhost:4000");
});
builder.Services.AddSingleton<GatewayClient>();
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<UserService>();
builder.Services.AddScoped<DeviceDbService>();
builder.Services.AddScoped<DeviceService>();
builder.Services.AddScoped<DoorService>();
builder.Services.AddScoped<DeviceGatewayService>();
builder.Services.AddScoped<UtilisateurRepository>();
builder.Services.AddScoped<DashboardService>();


builder.Services.AddGrpcServices(builder.Configuration);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null)));

builder.Services.AddCors(o =>
    o.AddPolicy("AllowLocalhost", p =>
        p.WithOrigins("https://localhost:7211")
         .AllowAnyMethod()
         .AllowAnyHeader()));

builder.Services.AddSession(o =>
{
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
    o.IdleTimeout = TimeSpan.FromDays(30);
});

await RunAsync();
async Task RunAsync()
{
    builder.Services.AddGrpc();
    var app = builder.Build();

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }
    else
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var gateway = scope.ServiceProvider.GetRequiredService<GatewayClient>();

        logger.LogInformation("Vérification de la connexion gRPC au démarrage de l'application");

        try
        {
            if (!gateway.IsConnected)
            {
                var success =  gateway.Connect("localhost", 4000);
                if (success)
                    logger.LogInformation("gRPC dev connecté");
                else
                    logger.LogError("Échec connexion gRPC dev");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur connexion gRPC: {Message}", ex.Message);
        }
    }

    app.UseCors("AllowLocalhost");
    app.MapGrpcService<ConnectSvc>();
    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();
    app.UseAuthorization();
    app.UseSession();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Welcome}/{id?}");
    app.Run();
}
