using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sportia.Models;

namespace Sportia.Controllers
{
    public class AdminReporteController : Controller
    {
        private readonly SportiaDbContext _context;

        public AdminReporteController(SportiaDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var totalClientes = await _context.Clientes.CountAsync();

            var totalUsuarios = await _context.Usuarios.CountAsync();

            var totalEmpresas = await _context.Empresas.CountAsync();

            var totalEscenarios = await _context.Escenarios.CountAsync();

            var totalReservas = await _context.Reservas.CountAsync();

            var totalPagos = await _context.Pagos.CountAsync();

            var ingresosTotales = await _context.Pagos
                .SumAsync(p => (decimal?)p.MontoPagado) ?? 0;

            var saldoPendiente = await _context.Pagos
                .SumAsync(p => (decimal?)p.SaldoPendiente) ?? 0;

            var reservasHoy = await _context.Reservas
                .CountAsync(r =>
                    r.FechaReserva != null &&
                    r.FechaReserva.Value.Date == DateTime.Today);

            ViewBag.TotalClientes = totalClientes;
            ViewBag.TotalUsuarios = totalUsuarios;
            ViewBag.TotalEmpresas = totalEmpresas;
            ViewBag.TotalEscenarios = totalEscenarios;
            ViewBag.TotalReservas = totalReservas;
            ViewBag.TotalPagos = totalPagos;
            ViewBag.IngresosTotales = ingresosTotales;
            ViewBag.SaldoPendiente = saldoPendiente;
            ViewBag.ReservasHoy = reservasHoy;

            return View();
        }
    }
}