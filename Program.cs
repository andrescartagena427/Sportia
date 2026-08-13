using Microsoft.EntityFrameworkCore;
using Sportia.Models;

var builder = WebApplication.CreateBuilder(args);

<<<<<<< HEAD
// Servicios MVC
builder.Services.AddControllersWithViews();

// ---------------------------------------------------------
// CONEXIÓN A MYSQL - SPORTIA
// ---------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("ConexionSportia");

builder.Services.AddDbContext<SportiaDbContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString)
    )
);

// ---------------------------------------------------------
// SESIONES
// ---------------------------------------------------------
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ---------------------------------------------------------
// CONSTRUCCIÓN DE LA APLICACIÓN
// ---------------------------------------------------------
var app = builder.Build();

// ---------------------------------------------------------
// MANEJO DE ERRORES
// ---------------------------------------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// ---------------------------------------------------------
// MIDDLEWARE
// ---------------------------------------------------------
app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

// IMPORTANTE: activar las sesiones
app.UseSession();

app.UseAuthorization();

// ---------------------------------------------------------
// RUTA PRINCIPAL
// ---------------------------------------------------------
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);
=======
// Add services to the container.
builder.Services.AddControllersWithViews();

// -------------------------------------------------------------------
// CONEXIÓN A MYSQL (SPORTIA DB)
// -------------------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("ConexionSportia");

builder.Services.AddDbContext<SportiaDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
);
// -------------------------------------------------------------------

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();
>>>>>>> aada5e8aef8b4681a63ef3455cee3bdcfb1c91ad

app.Run();