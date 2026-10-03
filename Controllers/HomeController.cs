using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sportia.Models;
using Sportia.Models.ViewModels;
using System.Diagnostics;

namespace Sportia.Controllers
{
    public class HomeController : Controller
    {
        private readonly SportiaDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(SportiaDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // =====================================================
        // PÁGINA PRINCIPAL (la ve todo el mundo)
        // =====================================================
        public async Task<IActionResult> Index()
        {
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");
            int? idRol = HttpContext.Session.GetInt32("IdRol");

            var vm = new InicioViewModel
            {
                HaySesion = idUsuario != null,
                EsCliente = idUsuario != null && idRol == 2
            };

            // Si la base de datos falla, la página igual se muestra (sin escenarios)
            try
            {
                var escenarios = await _context.Escenarios
                    .AsNoTracking()
                    .Include(e => e.IdTipoNavigation)
                    .Include(e => e.IdEmpresaNavigation)
                    .Where(e => e.Estado == true)
                    .OrderBy(e => e.Nombre)
                    .ToListAsync();

                vm.Escenarios = escenarios.Select(e => new EscenarioInicioViewModel
                {
                    IdEscenario = e.IdEscenario,
                    Nombre = e.Nombre,
                    Tipo = e.IdTipoNavigation?.Nombre ?? "Escenario",
                    Empresa = e.IdEmpresaNavigation?.Nombre ?? "",
                    Ubicacion = !string.IsNullOrWhiteSpace(e.IdEmpresaNavigation?.Direccion)
                        ? e.IdEmpresaNavigation!.Direccion!
                        : (e.IdEmpresaNavigation?.Nombre ?? "Sportia"),
                    Capacidad = e.Capacidad,
                    Precio = e.Precio,
                    Imagen = e.Imagen
                }).ToList();

                vm.Tipos = vm.Escenarios.Select(e => e.Tipo).Distinct().OrderBy(t => t).ToList();
                vm.Empresas = vm.Escenarios
                    .Select(e => e.Empresa)
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Distinct()
                    .OrderBy(n => n)
                    .ToList();

                vm.TotalCanchas = vm.Escenarios.Count;
                vm.TotalUsuarios = await _context.Clientes.CountAsync();
                vm.TotalReservas = await _context.Reservas
                    .CountAsync(r => !r.IdEstadoNavigation.Nombre.ToLower().Contains("cancel"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudieron cargar los datos de la página principal.");
            }

            return View(vm);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
