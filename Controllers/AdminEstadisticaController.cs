using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sportia.Models;

namespace Sportia.Controllers
{
    public class AdminEstadisticaController : Controller
    {
        private readonly SportiaDbContext _context;

        public AdminEstadisticaController(SportiaDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Estadísticas generales

            ViewBag.TotalClientes =
                await _context.Clientes.CountAsync();

            ViewBag.TotalEmpresas =
                await _context.Empresas.CountAsync();

            ViewBag.TotalEscenarios =
                await _context.Escenarios.CountAsync();

            ViewBag.TotalReservas =
                await _context.Reservas.CountAsync();


            // Total de dinero recibido

            ViewBag.Ingresos =
                await _context.Pagos
                    .SumAsync(p => (decimal?)p.MontoPagado) ?? 0;


            // Saldo pendiente

            ViewBag.SaldoPendiente =
                await _context.Pagos
                    .SumAsync(p => (decimal?)p.SaldoPendiente) ?? 0;


            return View();
        }
    }
}