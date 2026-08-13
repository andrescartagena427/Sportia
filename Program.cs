using Microsoft.EntityFrameworkCore;
using Sportia.Models;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------
// SERVICIOS MVC
// ---------------------------------------------------------
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

app.UseSession();

app.UseAuthorization();

// ---------------------------------------------------------
// RUTA PRINCIPAL
// ---------------------------------------------------------
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.Run();