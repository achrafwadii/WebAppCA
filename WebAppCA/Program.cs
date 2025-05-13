using WebAppCA.Services;
using System;
using System.IO;
using WebAppCA.Extensions;
using Microsoft.EntityFrameworkCore;
using WebAppCA.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using WebAppCA.Repositories;
using Grpc.Net.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Debug);


builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<UserService>();
builder.Services.AddScoped<DeviceDbService>();
builder.Services.AddScoped<DeviceService>();
builder.Services.AddScoped<DoorService>();
builder.Services.AddScoped<UtilisateurRepository>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddGrpcServices(builder.Configuration);

// Connexion gRPC sans validation SSL (mode dev uniquement)
var httpHandler = new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
};

var channel = GrpcChannel.ForAddress("https://localhost:4000", new GrpcChannelOptions
{
    HttpHandler = httpHandler
});

builder.Services.AddSingleton<ConnectSvc>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<ConnectSvc>>();
    return new ConnectSvc(channel, logger);
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null)
    ));

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
    logger.LogInformation("Vérification de la connexion gRPC au démarrage de l'application");

    try
    {
        var gateway = scope.ServiceProvider.GetRequiredService<GatewayClient>();
        if (!gateway.IsConnected)
        {
            // appel de l’overload insecure
            if (gateway.Connect("localhost", 4000))
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
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Welcome}/{id?}");

app.Run();
