using WebAppCA.Services;
using System;
using System.IO;
using System.Runtime.InteropServices;

var builder = WebApplication.CreateBuilder(args);

// Import SetDllDirectory
[DllImport("kernel32", SetLastError = true)]
static extern bool SetDllDirectory(string lpPathName);

// Chemin vers les DLL
var sdkDir = Path.Combine(AppContext.BaseDirectory, "BioStarSDK", "lib");

// Ajoute le dossier natif au chargement des DLL
SetDllDirectory(sdkDir);

// Vérifie et crée le dossier si nécessaire
if (!Directory.Exists(sdkDir))
{
    Directory.CreateDirectory(sdkDir);
    Console.WriteLine($"Création du dossier {sdkDir}");
}

// Ajoute au PATH pour les processus enfants
var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
if (!path.Contains(sdkDir))
{
    Environment.SetEnvironmentVariable("PATH", path + Path.PathSeparator + sdkDir);
    Console.WriteLine("SDK ajouté au PATH");
}

// Services
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<UserService>();

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
