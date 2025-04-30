using WebAppCA.Services;
using System;
using System.IO;
using System.Runtime.InteropServices;
using WebAppCA.Extensions;
using Microsoft.EntityFrameworkCore;
using WebAppCA.Data;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<UserService>();
builder.Services.AddScoped<DeviceDbService>();
builder.Services.AddScoped<ConnectSvc>();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure()
    ));
builder.Services.AddGrpcServices(builder.Configuration);

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
    // The default HSTS value is 30 days. You may want to change this for production scenarios.
    app.UseHsts();
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
