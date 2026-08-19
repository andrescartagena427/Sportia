using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sportia.Models;

namespace Sportia.Controllers
{
    public class DashboardController : Controller
    {
        private readonly SportiaDbContext _context;

        public DashboardController(SportiaDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // DASHBOARD ADMINISTRADOR
        // =====================================================
        public async Task<IActionResult> Index()
        {
            // =================================================
            // VERIFICAR SESIÓN
            // =================================================

            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");
            int? idRol = HttpContext.Session.GetInt32("IdRol");

            // Si no ha iniciado sesión
            if (idUsuario == null)
            {
                return RedirectToAction("Index", "Login");
            }

            // Si no es administrador
            if (idRol != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            // =================================================
            // TOTALES GENERALES
            // =================================================

            ViewBag.TotalUsuarios =
                await _context.Usuarios.CountAsync();

            ViewBag.TotalClientes =
                await _context.Clientes.CountAsync();

            ViewBag.TotalEscenarios =
                await _context.Escenarios.CountAsync();

            ViewBag.TotalReservas =
                await _context.Reservas.CountAsync();

            ViewBag.TotalEmpresas =
                await _context.Empresas.CountAsync();

            ViewBag.TotalPagos =
                await _context.Pagos.CountAsync();


            // =================================================
            // ESCENARIOS ACTIVOS
            // =================================================

            ViewBag.EscenariosActivos =
                await _context.Escenarios
                    .CountAsync(e => e.Estado == true);


            // =================================================
            // RESERVAS DE HOY
            // =================================================

            var hoy = DateOnly.FromDateTime(DateTime.Today);

            ViewBag.ReservasHoy =
                await _context.Reservas
                    .CountAsync(r => r.FechaUso == hoy);


            // =================================================
            // INGRESOS DEL MES
            // =================================================

            var inicioMes =
                new DateTime(
                    DateTime.Today.Year,
                    DateTime.Today.Month,
                    1
                );

            var siguienteMes =
                inicioMes.AddMonths(1);

            ViewBag.IngresosMes =
                await _context.Pagos
                    .Where(p =>
                        p.FechaPago != null &&
                        p.FechaPago >= inicioMes &&
                        p.FechaPago < siguienteMes
                    )
                    .SumAsync(p => (decimal?)p.MontoPagado)
                ?? 0;


            // =================================================
            // RESERVAS RECIENTES
            // =================================================

            ViewBag.ReservasRecientes =
                await _context.Reservas
                    .Include(r => r.IdClienteNavigation)
                    .Include(r => r.IdEscenarioNavigation)
                    .Include(r => r.IdEstadoNavigation)
                    .Include(r => r.Pagos)
                    .OrderByDescending(r => r.IdReserva)
                    .Take(5)
                    .ToListAsync();


            // =================================================
            // ESCENARIOS MÁS RESERVADOS
            // =================================================

            ViewBag.EscenariosMasReservados =
                await _context.Escenarios
                    .Include(e => e.Reservas)
                    .OrderByDescending(e => e.Reservas.Count)
                    .Take(5)
                    .ToListAsync();


            // =================================================
            // RESERVAS POR DÍA - ÚLTIMOS 7 DÍAS
            // =================================================

            var fechas =
                Enumerable.Range(0, 7)
                    .Select(i =>
                        DateOnly.FromDateTime(
                            DateTime.Today.AddDays(-6 + i)
                        )
                    )
                    .ToList();

            var reservasPorDia =
                new List<int>();

            foreach (var fecha in fechas)
            {
                var cantidad =
                    await _context.Reservas
                        .CountAsync(r =>
                            r.FechaUso == fecha
                        );

                reservasPorDia.Add(cantidad);
            }

            ViewBag.FechasGrafica = fechas;

            ViewBag.ReservasPorDia =
                reservasPorDia;


            // =================================================
            // MOSTRAR DASHBOARD
            // =================================================

            return View();
        }
    }
}