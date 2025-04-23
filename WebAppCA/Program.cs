using WebAppCA.Services;
using System;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// V�rification et configuration du dossier SDK
string sdkPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BioStarSDK");
string libPath = Path.Combine(sdkPath, "lib");

if (!Directory.Exists(libPath))
{
    Directory.CreateDirectory(libPath);
    Console.WriteLine($"Dossier SDK cr��: {libPath}");
}

// Ajout de libPath au PATH pour que le SDK puisse trouver les dll d�pendantes
string pathVariable = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
if (!pathVariable.Contains(libPath))
{
    Environment.SetEnvironmentVariable("PATH", pathVariable + Path.PathSeparator + libPath);
    Console.WriteLine("Chemin SDK ajout� aux variables d'environnement");
}

// Add services
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<UserService>();
builder.Services.AddSingleton<BioStarService>();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromDays(30); // Session longue
});

var app = builder.Build();

// Configure pipeline