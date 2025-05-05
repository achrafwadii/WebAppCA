using WebAppCA.Services;
using System;
using System.IO;
using System.Runtime.InteropServices;
using WebAppCA.Extensions;
using Microsoft.EntityFrameworkCore;
using WebAppCA.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using WebAppCA.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Configuration des logs
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// Configuration et services de base
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<UserService>();
builder.Services.AddScoped<DeviceDbService>();
builder.Services.AddScoped<UtilisateurRepository>();

// Configuration gRPC
// Important: AddGrpcServices doit être appelé AVANT d'ajouter ConnectSvc
builder.Services.AddGrpcServices(builder.Configuration);
builder.Services.AddScoped<ConnectSvc>();

// Configuration de la base de données
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null)
    ));

// Configuration CORS
builder.Services.AddCors(o =>
    o.AddPolicy("AllowLocalhost", p =>
        p.WithOrigins("https://localhost:7211")
         .AllowAnyMethod()
         .AllowAnyHeader()));

// Configuration de session
builder.Services.AddSession(o =>
{
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
    o.IdleTimeout = TimeSpan.FromDays(30);
});

var app = builder.Build();

// Configuration de l'environnement
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    // En mode dev, vérifions la connexion gRPC dès le démarrage
    using (var scope = app.Services.CreateScope())
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Vérification de la connexion gRPC au démarrage de l'application");

        try
        {
            var gatewayClient = scope.ServiceProvider.GetRequiredService<GatewayClient>();
            if (!gatewayClient.IsConnected)
            {
                var certPath = builder.Configuration.GetValue<string>("GrpcSettings:CaCertPath") ?? "";
                var address = builder.Configuration.GetValue<string>("GrpcSettings:Address") ?? "localhost";
                var port = builder.Configuration.GetValue<int>("GrpcSettings:Port", 51211);

                logger.LogWarning("GatewayClient n'est pas connecté au démarrage, tentative de connexion...");
                var connected = gatewayClient.Connect(certPath, address, port);

                if (connected)
                {
                    logger.LogInformation("Connexion gRPC établie avec succès au démarrage de l'application");
                }
                else
                {
                    logger.LogError("Impossible d'établir la connexion gRPC au démarrage de l'application. " +
                                    "Vérifiez que le serveur gRPC est en cours d'exécution sur {Address}:{Port}", address, port);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la vérification/initialisation de la connexion gRPC: {Message}", ex.Message);
        }
    }
}

app.UseCors("AllowLocalhost");
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.UseSession();

app.MapControllerRoute(
    "default",
    "{controller=Home}/{action=Welcome}/{id?}");

app.Run();